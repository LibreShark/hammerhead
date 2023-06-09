using System.Text.RegularExpressions;
using Google.Protobuf;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.GameBoyColor;

/// <summary>
/// Code Breaker for Game Boy Color and Game Boy Pocket,
/// made by Future Console Design (FCD) and Pelican Accessories.
///
/// This device was also repackaged and rebranded as
/// "BrainBoy" and "Monster Brain", which were cheat devices tailored
/// specifically for Pokemon games. They had hard-coded cheats that were not
/// user-editable.
/// </summary>
public sealed class GbcCbRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyColor;
    private const CodecId ThisCodecId = CodecId.GbcCodebreakerRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static GbcCbRom Create(string filePath, u8[] rawInput)
    {
        return new GbcCbRom(filePath, rawInput);
    }

    private const u32 CheatNameListAddr    = 0x000067F0;
    private const u32 GameListAddr         = 0x00022000;
    private const u32 SelectedGameNameAddr = 0x0003C680;

    private readonly RomString[] _cheatNames = new RomString[16];
    private readonly RomString _selectedGameName;

    public override CodecId DefaultCheatOutputCodec => CodecId.HammerheadJson;

    private GbcCbRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsFirmware = true;
        Support.SupportsUserPrefs = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;
        _selectedGameName = Scribe.Seek(SelectedGameNameAddr).ReadPrintableCString().Trim();
        // TODO(CheatoBaggins): Are there any other preferences we need to check?
        Support.HasPristineUserPrefs = _selectedGameName.Value.Length == 0;

        Metadata.BrandId = DetectBrand(rawInput);

        RomString romId = Scribe.Seek(0).ReadPrintableCString().Trim();
        Metadata.Identifiers.Add(romId);
        Metadata.Identifiers.Add(_selectedGameName);

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
                IsGameActive = gameName.Value == _selectedGameName.Value,
            };

            for (u8 cheatIdx = 0; cheatIdx < 16; cheatIdx++)
            {
                u8[] code = Scribe.ReadBytes(4);
                u8 nameIdx = Scribe.ReadU8();
                if (code.IsPadding())
                {
                    continue;
                }

                RomString curCheatName =
                    nameIdx == 0
                        // TODO(CheatoBaggins): Are custom names stored in the ROM?
                        ? "USR CSTM".ToRomString() // Custom, user-entered cheat
                        : _cheatNames[nameIdx]; // Build-in cheat with standard name

                Cheat curCheat;
                Cheat? prevCheat = game.Cheats.LastOrDefault();
                if (prevCheat?.CheatName.Value == curCheatName.Value)
                {
                    curCheat = prevCheat;
                }
                else
                {
                    curCheat = new Cheat()
                    {
                        CheatIndex = cheatIdx,
                        CheatName = curCheatName,
                    };
                    game.Cheats.Add(curCheat);
                }

                curCheat.Codes.Add(new Code()
                {
                    CodeIndex = 0,
                    Bytes = ByteString.CopyFrom(code),
                });
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

    public override ICodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        bool is512KiB = bytes.IsKiB(512);
        return (is256KiB || is512KiB) && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        return DetectBrand(bytes) != BrandId.UnknownBrand;
    }

    private static BrandId DetectBrand(u8[] bytes)
    {
        string id = bytes[..0x20].ToAsciiString();
        if (id.Contains("CodeBreaker / GB"))
        {
            return BrandId.CodeBreaker;
        }
        return BrandId.UnknownBrand;
    }

    public static bool Is(ICodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }

    private static AbstractBinaryScribe MakeScribe(u8[] bytes)
    {
        return new LittleEndianScribe(bytes);
    }
}
