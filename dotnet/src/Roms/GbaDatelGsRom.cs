namespace LibreShark.Hammerhead;

// TODO(CheatoBaggins): Confirm whether this is actually made by Datel or FCD.
public sealed class GbaDatelGsRom : Rom
{
    private const RomType ThisRomType = RomType.GbaFcdGameshark;

    public GbaDatelGsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
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
        return rom.Type == ThisRomType;
    }

    public static bool Is(RomType type)
    {
        return type == ThisRomType;
    }

    public override void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"GBA Datel GameShark ROM file: '{FilePath}'");
    }
}
