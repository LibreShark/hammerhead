namespace LibreShark.Hammerhead.Roms;

/// <summary>
/// GameShark and Action Replay for Game Boy Advance,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbaGsDatelRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.GbaGamesharkDatel;

    public GbaGsDatelRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
        var minorVersionNumber = Bytes[0x21004];
        var majorVersionNumber = Bytes[0x21005];
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        return is256KiB && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        bool hasMagicNumber = bytes[..4].SequenceEqual(new byte[] { 0x2E, 0x00, 0x00, 0xEA });
        bool hasMagicText = bytes[0x21000..0x21004].ToUtf8String().Equals("GBA_");
        return hasMagicNumber && hasMagicText;
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
        Console.WriteLine($"GBA Datel GameShark ROM file: '{Metadata.FilePath}'");
    }
}
