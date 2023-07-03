using Google.Protobuf;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Codecs;

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
/// v4.x GameShark and Action Replay for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcGsV4Rom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyColor;
    private const CodecId ThisCodecId = CodecId.GbcGamesharkV4Rom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static GbcGsV4Rom Create(string filePath, u8[] rawInput)
    {
        return new GbcGsV4Rom(filePath, rawInput);
    }

    private const u32 TitleAddr = 0x00000134;
    private const u32 VerNumAddr = 0x00000143;
    private const u32 GameListAddr = 0x00008000;
    private const u32 CheatListAddr = 0x0000A000;

    private static readonly string[] KnownTitles =
    {
        "Action Replay V4.01",
        "Action Replay V4.2",
        "Gameshark     V4.2",
        "Gameshark     V4.0",
        "Gameshark     V4.2",
    };

    public override CodecId DefaultCheatOutputCodec => CodecId.HammerheadJson;

    private GbcGsV4Rom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsFirmware = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;

        // TODO(CheatoBaggins): Detect
        Support.SupportsUserPrefs = true;
        Support.HasPristineUserPrefs = false;

        Metadata.BrandId = IsGs(Buffer) ? BrandId.Gameshark : IsAr(Buffer) ? BrandId.ActionReplay : BrandId.UnknownBrand;
        Metadata.SortableVersion = ReadVersionNumber();
        Metadata.DisplayVersion = $"v{Metadata.SortableVersion:F2}";
        Metadata.LanguageIetfCode = Metadata.BrandId switch
        {
            BrandId.Gameshark => "en-US",
            BrandId.ActionReplay => "en-GB",
            _ => "und",
        };

        RomString title = Scribe.Seek(TitleAddr).ReadCStringUntilNull();
        Metadata.Identifiers.Add(title);
        Metadata.IsKnownVersion = KnownTitles.Contains(title.Value);

        Scribe.Seek(GameListAddr);
        ReadGames();

        Scribe.Seek(CheatListAddr);
        ReadCheatsBlock();
        ReadCheatsBlock();
        ReadCheatsBlock();
    }

    private void ReadGames()
    {
        u8[] unknownBytes1 = Scribe.ReadBytes(2);
        // The number 455 is hard-coded, and space is always pre-allocated in the ROM file
        for (u16 gameIdx = 0; gameIdx < 455; gameIdx++)
        {
            u16 gameNumberAndBitMask = Scribe.ReadU16();
            u16 gameNumber = (u16)(gameNumberAndBitMask & 0x01FFu);
            u16 bitMask = (u16)(gameNumberAndBitMask & ~0x01FFu);
            // TODO(CheatoBaggins): What is this flag, and why do only some ROMs have it set?
            bool isMSBSet       = (bitMask & 0x8000) > 0;
            bool isGameSelected = (bitMask & 0x2000) > 0;
            RomString gameName = Scribe.ReadPrintableCString(16, false).Trim();
            if (gameName.Value.Length == 0)
            {
                break;
            }

            string selectedStr = isGameSelected ? $" <!------------ CURRENTLY SELECTED GAME? bitmask (BE) = 0x{bitMask:X4}" : "";
            // Console.WriteLine($"games[{i:D3}]: 0x{gameNumberAndBitMask:X4} (BE) = {gameNumber:D0} ('{gameName.Value}'){selectedStr}");
            Games.Add(new Game()
            {
                GameIndex = gameIdx,
                GameName = gameName,
                IsGameActive = isGameSelected,
            });
        }
    }

    private void ReadCheatsBlock()
    {
        u8[] unknownBytes1 = Scribe.ReadBytes(2);

        // this number is hard-coded and pre-allocated in the ROM file
        for (u16 i = 0; i < 455; i++)
        {
            u32 cheatStartPos = Scribe.Position;

            u16 gameNumberAndBitMask = Scribe.ReadU16();
            RomString cheatName = Scribe.ReadPrintableCString(12, false).Trim();
            u8[] code = Scribe.ReadBytes(4);
            if (cheatName.Value.Length == 0)
            {
                break;
            }

            u16 gameNumber = (u16)(gameNumberAndBitMask & 0x01FFu);
            u16 bitMask = (u16)(gameNumberAndBitMask & ~0x01FFu);
            // The raw value is 1-indexed, so we need to subtract one for array access
            s32 gameIndex = gameNumber - 1;

            if (gameIndex < Games.Count)
            {
                Game game = Games[gameIndex];
                Cheat? cheat = game.Cheats.ToList().Find(c => c.CheatName.Value == cheatName.Value);
                if (cheat == null)
                {
                    cheat = new Cheat()
                    {
                        CheatIndex = (u32)game.Cheats.Count,
                        CheatName = cheatName,
                    };
                    game.Cheats.Add(cheat);
                }
                cheat.Codes.Add(new Code()
                {
                    CodeIndex = (u32)cheat.Codes.Count,
                    Bytes = ByteString.CopyFrom(code),
                });
                cheat.IsCheatActive = cheat.IsCheatActive || bitMask > 0;
            }
            else
            {
                Console.Error.WriteLine($"WARNING: Game #{gameNumber} [{gameIndex}] not found in list! " +
                                        $"Cheat '{cheatName.Value}' at 0x{cheatStartPos:X8}. " +
                                        $"Bitmask (BE) = 0x{bitMask:X4}.");
            }
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
        bool hasMagicNumber = bytes[..4].SequenceEqual(new u8[] { 0xC3, 0x50, 0x01, 0x78 });
        bool hasIdentifier = IsGs(bytes) || IsAr(bytes);
        // TODO(CheatoBaggins): Determine whether the magic number is identical in all ROMs
        return /*hasMagicNumber &&*/ hasIdentifier;
    }

    private static bool IsGs(u8[] bytes)
    {
        string identifier = bytes[(int)TitleAddr..(int)(TitleAddr + 18)].ToAsciiString();
        return identifier.StartsWith("Gameshark     V") ||
               // TODO(CheatoBaggins): Determine whether this string also appears in Action Replay ROMs
               identifier.StartsWith("DAMON BARWIN");
    }

    private static bool IsAr(u8[] bytes)
    {
        string identifier = bytes[(int)TitleAddr..(int)(TitleAddr + 18)].ToAsciiString();
        return identifier.StartsWith("Action Replay V");
    }

    private double ReadVersionNumber()
    {
        RomString verNumRaw = Scribe.Seek(VerNumAddr).ReadPrintableCString(0, true);
        if (Double.TryParse(verNumRaw.Value, out double verNumParsed))
        {
            return verNumParsed;
        }
        return -1;
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
