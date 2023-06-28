using Google.Protobuf;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Roms;

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
/// GameShark and Action Replay for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcGsRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.GameBoyColor;
    private const RomFormat ThisRomFormat = RomFormat.GbcGameshark;

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

    public GbcGsRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
        Metadata.Brand = IsGs(Buffer) ? RomBrand.Gameshark : IsAr(Buffer) ? RomBrand.ActionReplay : RomBrand.UnknownBrand;
        Metadata.SortableVersion = ReadVersionNumber();
        Metadata.DisplayVersion = $"v{Metadata.SortableVersion:F2}";
        Metadata.LanguageIetfCode = Metadata.Brand switch
        {
            RomBrand.Gameshark => "en-US",
            RomBrand.ActionReplay => "en-GB",
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

    public static bool Is(u8[] bytes)
    {
        bool is128KiB = bytes.IsKiB(128);
        return is128KiB && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        bool hasMagicNumber = bytes[..4].SequenceEqual(new u8[] { 0xC3, 0x50, 0x01, 0x78 });
        bool hasIdentifier = IsGs(bytes) || IsAr(bytes);
        return hasMagicNumber && hasIdentifier;
    }

    private static bool IsGs(u8[] bytes)
    {
        string identifier = bytes[(int)TitleAddr..(int)(TitleAddr + 18)].ToAsciiString();
        return identifier.StartsWith("Gameshark     V");
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

    public static bool Is(Rom rom)
    {
        return rom.Metadata.RomFormat == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    private static BinaryScribe MakeScribe(u8[] rawInput)
    {
        return new LittleEndianScribe(rawInput.ToArray());
    }
}
