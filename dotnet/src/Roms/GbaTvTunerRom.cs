namespace LibreShark.Hammerhead.Roms;

/// <summary>
/// TV Tuner for Game Boy Advance, made by Blaze and Pelican Accessories.
/// There are NTSC and PAL variants.
/// </summary>
public sealed class GbaTvTunerRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.GbaTvTuner;

    public GbaTvTunerRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
    }

    public static bool Is(byte[] bytes)
    {
        bool is512KiB = bytes.IsKiB(512);
        bool is16MiB = bytes.IsMiB(16);
        return (is512KiB || is16MiB) && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        var idBytes = bytes[0xA0..0xAB];
        var idStr = idBytes.ToAsciiString();
        return idStr == "GBA_Capture";
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
