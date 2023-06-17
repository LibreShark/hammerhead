namespace LibreShark.Hammerhead;

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
        bool is512KiB = bytes.Length == 0x00080000;
        bool is16MiB = bytes.Length == 0x01000000;
        return (is512KiB || is16MiB) && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        var idBytes = bytes[0xA0..0xAB];
        var idStr = idBytes.ToUtf8String();
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
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"GBA Blaze/Pelican TV Tuner ROM file: '{Metadata.FilePath}'");
    }
}
