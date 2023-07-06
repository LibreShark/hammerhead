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
/// v3.X GameShark and Action Replay for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct in 2000 and 2001.
///
/// The 2000 release has a sticker on the box and cart that says "v3.1", while
/// the PC software says "v0.80".
///
/// The 2001 release does not have any stickers on the box or cart, but the
/// CD says "v4.10" and the PC software says "v4.00".
///
/// Both releases have semi-transparent clear plastic shells on the carts,
/// and purple "GameShark" stickers.
/// </summary>
/// <seealso cref="GbcGsV3CodeDb"/>
/// <seealso cref="GbcGsV3CodeFile"/>
public sealed class GbcGsV3Rom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyColor;
    private const CodecId ThisCodecId = CodecId.GbcGamesharkV3Rom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static GbcGsV3Rom Create(string filePath, u8[] rawInput)
    {
        return new GbcGsV3Rom(filePath, rawInput);
    }

    private const u32 CartIdAddr = 0x00000134;
    private const u32 GameListAddr = 0x00010000;

    private static readonly string[] KnownDisplayVersions =
    {
        "v3.00",
    };

    public override CodecId DefaultCheatOutputCodec => CodecId.HammerheadJson;

    private GbcGsV3Rom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsFirmware = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;

        Metadata.BrandId =
            IsGs(Buffer)
                ? BrandId.Gameshark
                : IsAr(Buffer)
                    ? BrandId.ActionReplay
                    : BrandId.UnknownBrand;
        Metadata.SortableVersion = ReadVersionNumber();
        Metadata.DisplayVersion = $"v{Metadata.SortableVersion:F2}";
        Metadata.LanguageIetfCode = Metadata.BrandId switch
        {
            BrandId.Gameshark => "en-US",
            BrandId.ActionReplay => "en-GB",
            _ => "und",
        };

        RomString cartId = Scribe.Seek(CartIdAddr).ReadPrintableCString();
        Metadata.Identifiers.Add(cartId);

        s32 firstGsAddr = Scribe.Find("GameShark");
        s32 firstArAddr = Scribe.Find("Action Replay");
        if (firstGsAddr > -1)
        {
            RomString productName = Scribe.Seek(firstGsAddr).ReadPrintableCString();
            Metadata.Identifiers.Add(productName);
        }
        if (firstArAddr > -1)
        {
            RomString productName = Scribe.Seek(firstArAddr).ReadPrintableCString();
            Metadata.Identifiers.Add(productName);
        }

        // TODO(CheatoBaggins): Implement
        Metadata.IsKnownVersion = KnownDisplayVersions.Contains(Metadata.DisplayVersion);

        ReadGames();
    }

    private double ReadVersionNumber()
    {
        u8 major = Buffer[0x148];
        u8 minor = Buffer[0x149];
        return Double.Parse($"{major}.{minor}");
    }

    private void ReadGames()
    {
        Scribe.Seek(GameListAddr);

        while (!Scribe.IsPadding())
        {
            u32 gameStartAddr = Scribe.Position;
            RomString gameName = Scribe.ReadPrintableCString(16, false).Trim();
            Scribe.Seek(gameStartAddr + 16);

            // Empty game name
            if (string.IsNullOrWhiteSpace(gameName.Value))
            {
                // Skip to the next game
                Scribe.Seek(gameStartAddr + 0x80);
                continue;
            }

            var game = new Game()
            {
                GameIndex = (u32)Games.Count,
                GameName = gameName,
            };
            Games.Add(game);

            while (!Scribe.IsPadding() && Scribe.Position < gameStartAddr + 0x80)
            {
                u32 cheatStartAddr = Scribe.Position;

                Cheat? prevCheat = game.Cheats.LastOrDefault();
                Cheat curCheat;
                RomString curCheatName = Scribe.ReadPrintableCString(12, false).Trim();
                Scribe.Seek(cheatStartAddr + 12);

                // Empty cheat name
                if (string.IsNullOrWhiteSpace(curCheatName.Value))
                {
                    // Skip to the next cheat
                    Scribe.Seek(cheatStartAddr + 0x10);
                    continue;
                }

                if (curCheatName.Value == prevCheat?.CheatName.Value)
                {
                    // Append code to existing cheat entry with the same name
                    curCheat = prevCheat;
                }
                else
                {
                    curCheat = new Cheat()
                    {
                        CheatIndex = (u32)game.Cheats.Count,
                        CheatName = curCheatName,
                    };
                    game.Cheats.Add(curCheat);
                }

                u8[] bytes = Scribe.ReadBytes(4);
                var code = new Code()
                {
                    CodeIndex = (u32)curCheat.Codes.Count,
                    Bytes = ByteString.CopyFrom(bytes),
                };
                curCheat.Codes.Add(code);
            }

            // Jump to the next game. Every game has a fixed size in the ROM.
            Scribe.Seek(gameStartAddr + 0x80);
        }
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        bool is128KiB = bytes.IsKiB(128);
        return is128KiB && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        string cartId = bytes[(s32)CartIdAddr..(s32)(CartIdAddr + 12)].ToAsciiString();
        bool isDamonCartId = cartId == "DAMON BARWIN";
        bool isDamonEmail = bytes.Contains("damon@datel.co.uk");
        bool isKnownBrand = IsGs(bytes) || IsAr(bytes);
        return isDamonCartId && isDamonEmail && isKnownBrand;
    }

    private static bool IsGs(u8[] bytes)
    {
        return bytes.Contains("GameShark") ||
               bytes.Contains("GAMESHARK");
    }

    private static bool IsAr(u8[] bytes)
    {
        return bytes.Contains("Action Replay") ||
               bytes.Contains("ACTION REPLAY");
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
