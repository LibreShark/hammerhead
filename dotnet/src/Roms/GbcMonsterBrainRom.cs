namespace LibreShark.Hammerhead.Roms;

/// <summary>
/// Monster Brain and Brain Boy for Game Boy Color and Game Boy Pocket,
/// made by Future Console Design (FCD) and Pelican Accessories.
/// </summary>
public sealed class GbcMonsterBrainRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.GbcMonsterbrain;

    public GbcMonsterBrainRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        bool is512KiB = bytes.Length == 0x00080000;
        return (is256KiB || is512KiB) && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        string identifier = bytes[0x00..0x20].ToAsciiString();
        return identifier.StartsWith("BrainBoy") ||
               identifier.StartsWith("Monster Brain");
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
