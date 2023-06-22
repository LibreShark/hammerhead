using System.Text.RegularExpressions;
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
/// Code Breaker for Game Boy Color and Game Boy Pocket,
/// made by Future Console Design (FCD) and Pelican Accessories.
///
/// This device was also repackaged and rebranded as
/// "BrainBoy" and "Monster Brain", which were cheat devices tailored
/// specifically for Pokemon games. They had hard-coded cheats that were not
/// user-editable.
/// </summary>
public sealed class GbcCbRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.GameBoyColor;
    private const RomFormat ThisRomFormat = RomFormat.GbcCodebreaker;

    private const u32 CheatNameListAddr    = 0x000067F0;
    private const u32 GameListAddr         = 0x00022000;
    private const u32 SelectedGameNameAddr = 0x0003C680;

    private readonly RomString[] _cheatNames = new RomString[16];

    public GbcCbRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
        Metadata.Brand = DetectBrand(rawInput);

        RomString romId = Scribe.Seek(0).ReadPrintableCString().Trim();
        RomString selectedGameName = Scribe.Seek(SelectedGameNameAddr).ReadPrintableCString().Trim();

        Metadata.Identifiers.Add(romId);
        Metadata.Identifiers.Add(selectedGameName);

        ParseVersion(romId);
        ParseGames();
    }

    private void ParseGames()
    {
        Scribe.Seek(CheatNameListAddr);
        for (u8 nameIdx = 0; nameIdx < _cheatNames.Length; nameIdx++)
        {
            _cheatNames[nameIdx] = Scribe.ReadCStringUntilNull(8, false).Trim();
        }

        Scribe.Seek(GameListAddr);
        for (u8 gameIdx = 0; gameIdx < 255; gameIdx++)
        {
            u32 gameStartPos = Scribe.Position;
            RomString gameName = Scribe.ReadCStringUntilNull(15, true).Trim();
            if (gameName.Value.Length == 0)
            {
                Scribe.Seek(gameStartPos + 0x60);
                continue;
            }

            Scribe.Seek(gameStartPos + 0x10);

            var game = new Game()
            {
                GameIndex = gameIdx,
                GameName = gameName,
            };

            for (u8 cheatIdx = 0; cheatIdx < 16; cheatIdx++)
            {
                u8[] code = Scribe.ReadBytes(4);
                u8 nameIdx = Scribe.ReadU8();
                if (code.IsPadding())
                {
                    continue;
                }

                var cheat = new Cheat()
                {
                    CheatIndex = cheatIdx,
                    CheatName = nameIdx == 0
                        // Custom, user-entered cheat
                        // TODO(CheatoBaggins): Are custom names stored in the ROM?
                        ? new RomString() { Value = "USR CSTM" }
                        // Build-in cheat with standard name
                        : _cheatNames[nameIdx],
                };
                cheat.Codes.Add(new Code()
                {
                    CodeIndex = 0,
                    Bytes = ByteString.CopyFrom(code),
                });
                game.Cheats.Add(cheat);
            }

            Games.Add(game);
        }
    }

    private void ParseVersion(RomString romId)
    {
        Match match = Regex.Match(romId.Value, @"(?:v|version )(?<number>\d+\.\d+)(?<decorators>.*)");
        if (!match.Success)
        {
            return;
        }

        string numberStr = match.Groups["number"].Value.Trim();
        string decoratorStr = match.Groups["decorators"].Value.Trim();
        if (decoratorStr.Length > 1)
        {
            decoratorStr = " " + decoratorStr;
        }

        Metadata.DisplayVersion = $"v{numberStr}{decoratorStr}".Trim();
        Metadata.SortableVersion = Double.Parse(numberStr);

        if (decoratorStr.Length == 1)
        {
            char c = decoratorStr.ToLower()[0];
            int d = c - 0x60;

            // E.g., "v1.0c" -> "v1.03"
            Metadata.SortableVersion = Double.Parse($"{numberStr}{d}");
        }
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        bool is512KiB = bytes.IsKiB(512);
        return (is256KiB || is512KiB) && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        return DetectBrand(bytes) != RomBrand.UnknownBrand;
    }

    private static RomBrand DetectBrand(u8[] bytes)
    {
        string id = bytes[..0x20].ToAsciiString();
        if (id.Contains("CodeBreaker / GB"))
        {
            return RomBrand.CodeBreaker;
        }
        return RomBrand.UnknownBrand;
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Format == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    private static BinaryScribe MakeScribe(u8[] bytes)
    {
        return new LittleEndianScribe(bytes);
    }

    protected override void PrintCustomHeader()
    {
    }
}
