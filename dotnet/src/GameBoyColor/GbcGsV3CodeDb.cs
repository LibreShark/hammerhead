using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Google.Protobuf;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.GameBoyColor;

// ReSharper disable BuiltInTypeReferenceStyle
using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using s64 = Int64;
using u64 = UInt64;
using f64 = Double;

/// <summary>
/// Cheat database from Datel's official PC software for
/// GameShark and Action Replay for Game Boy Color, made in 2000 and 2001.
///
/// The 2000 release has a sticker on the box and cart that says "v3.1", while
/// the PC software says "v0.80".
///
/// The 2001 release does not have any stickers on the box or cart, but the
/// CD says "v4.10" and the PC software says "v4.00".
///
/// Both releases have semi-transparent clear plastic shells on the carts,
/// and purple "GameShark" stickers.
///
/// The 2001 release has newer codes pre-installed on the cart and in the
/// PC software's cheat database, but otherwise the PC software appears to be
/// essentially identical.
///
/// File location:
/// <c>C:\Program Files\Interact\GameShark for GameBoy\gbdata\gbcheats.bin</c>
/// </summary>
/// <seealso cref="GbcGsV3CodeFile"/>
/// <seealso cref="GbcGsV3Rom"/>
public sealed class GbcGsV3CodeDb : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyColor;
    private const CodecId ThisCodecId = CodecId.GbcGamesharkV3Cdb;

    private static readonly Regex StandardCodeRegex =
        new Regex("^[a-f0-9]{8}$", RegexOptions.IgnoreCase);
    private static readonly Regex PlaceholderCodeRegex =
        new Regex("^[a-f0-9]{2}xx[a-f0-9]{4}$", RegexOptions.IgnoreCase);

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static GbcGsV3CodeDb Create(string filePath, u8[] rawInput)
    {
        return new GbcGsV3CodeDb(filePath, rawInput);
    }

    public override CodecId DefaultCheatOutputCodec => ThisCodecId;

    private readonly bool _isSilent;

    private GbcGsV3CodeDb(string filePath, u8[] rawInput, bool isSilent = false)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        _isSilent = isSilent;

        Support.SupportsCheats = true;

        ReadGames();

        // TODO(CheatoBaggins): Determine if Action Replay-branded software
        // exists, and if it's possible to detect the brand from file contents.
        Metadata.BrandId = BrandId.Gameshark;
    }

    private void PrintWarning(Exception e)
    {
        if (_isSilent)
        {
            return;
        }

        var printer = new TerminalPrinter(this);
        printer.PrintWarning(e.Message);
    }

    private void ReadGames()
    {
        Scribe.Seek(0);
        u16 gameCount = Scribe.ReadU16();
        var gameMap = new Dictionary<u16, Game>(gameCount);
        for (u16 i = 0; i < gameCount; i++)
        {
            u16 rawGameNumber = Scribe.ReadU16();

            // TODO(CheatoBaggins): Figure out what these flags mean
            u16 gameNumber = (u16)(rawGameNumber & ~0x9000);

            RomString gameName = Scribe.ReadFixedLengthPrintableCString(16);
            var game = new Game()
            {
                GameIndex = (u32)gameMap.Count,
                GameName = gameName,
            };
            gameMap[gameNumber] = game;
            Games.Add(game);
        }

        u16 cheatCount = Scribe.ReadU16();
        for (u16 i = 0; i < cheatCount; i++)
        {
            u16 rawGameNumber = Scribe.ReadU16();

            // TODO(CheatoBaggins): Figure out what these flags mean
            bool isCheatActive = (rawGameNumber & 0x9000) != 0;
            u16 gameNumber = (u16)(rawGameNumber & ~0x9000);

            RomString cheatName = Scribe.ReadFixedLengthPrintableCString(12);
            u32 codeAddr = Scribe.Position;
            RomString codeStr = Scribe.ReadFixedLengthPrintableCString(8);

            if (codeStr.Value.Length < 8)
            {
                PrintWarning(
                    new FormatException(
                        $"WARNING: Invalid cheat code length at 0x{codeAddr:X8}: '{codeStr.Value}' ({cheatName.Value})."
                    )
                );
                continue;
            }

            // GBC GameShark PC software v4.x has at least one typo where
            // the letter 'O' is used instead of the number zero,
            // which causes a parsing error if we don't replace it with zero.
            codeStr.Value = codeStr.Value.Replace('O', '0');
            codeStr.Value = codeStr.Value.Replace('o', '0');

            if (!StandardCodeRegex.IsMatch(codeStr.Value))
            {
                if (PlaceholderCodeRegex.IsMatch(codeStr.Value))
                {
                    PrintWarning(
                        new FormatException(
                            $"WARNING: Unsupported value placeholder at 0x{codeAddr:X8}: '{codeStr.Value}' ({cheatName.Value})."
                        )
                    );
                }
                else
                {
                    PrintWarning(
                        new FormatException(
                            $"WARNING: Invalid cheat code characters at 0x{codeAddr:X8}: '{codeStr.Value}' ({cheatName.Value})."
                        )
                    );
                }
                continue;
            }

            bool hasKey = gameMap.ContainsKey(gameNumber);
            Game game = gameMap[gameNumber];
            Cheat? prevCheat = game.Cheats.LastOrDefault();
            byte[] codeBytes = codeStr.Value.HexToBytes();
            if (prevCheat?.CheatName.Value == cheatName.Value)
            {
                var code = new Code()
                {
                    CodeIndex = (u32)prevCheat.Codes.Count,
                    Bytes = ByteString.CopyFrom(codeBytes),
                };
                prevCheat.Codes.Add(code);
            }
            else
            {
                var curCheat = new Cheat()
                {
                    CheatIndex = (u32)game.Cheats.Count,
                    CheatName = cheatName,
                    IsCheatActive = isCheatActive,
                };
                game.Cheats.Add(curCheat);
                var code = new Code()
                {
                    CodeIndex = (u32)curCheat.Codes.Count,
                    Bytes = ByteString.CopyFrom(codeBytes),
                };
                curCheat.Codes.Add(code);
            }
        }
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        // TODO(CheatoBaggins): Is there a way to detect these?
        try
        {
            var codec = new GbcGsV3CodeDb("", bytes, true);
            return codec.Games.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool Is(AbstractCodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }

    private static AbstractBinaryScribe MakeScribe(u8[] rawInput)
    {
        return new LittleEndianScribe(rawInput.ToArray());
    }
}
