using System.Text.RegularExpressions;

namespace LibreShark.Hammerhead.Roms;

/// <summary>
/// Code Breaker for Game Boy Color and Game Boy Pocket,
/// made by Future Console Design (FCD) and Pelican Accessories.
/// </summary>
public sealed class GbcCbRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.GameBoyColor;
    private const RomFormat ThisRomFormat = RomFormat.GbcCodebreaker;

    public GbcCbRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisConsole, ThisRomFormat)
    {
        // CodeBreaker v1.0c

        Metadata.Brand = DetectBrand(Bytes);

        string id = bytes[..0x20].ToAsciiString();
        Match match = Regex.Match(id, @"(?:v|version )(?<number>\d+\.\d+)(?<decorators>.*)");
        if (match.Success)
        {
            string numberStr = match.Groups["number"].Value.Trim();
            string decoratorStr = match.Groups["decorators"].Value.Trim();
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
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        bool is512KiB = bytes.IsKiB(512);
        return (is256KiB || is512KiB) && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        return DetectBrand(bytes) != RomBrand.UnknownBrand;
    }

    private static RomBrand DetectBrand(byte[] bytes)
    {
        string id = bytes[..0x20].ToAsciiString();
        if (id.Contains("CodeBreaker / GB"))
        {
            return RomBrand.CodeBreaker;
        }
        if (id.Contains("BrainBoy"))
        {
            return RomBrand.Brainboy;
        }
        if (id.Contains("Monster Brain"))
        {
            return RomBrand.MonsterBrain;
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

    protected override void PrintCustomHeader()
    {
    }
}
