namespace LibreShark.Hammerhead;

/// <summary>
/// GameShark and Action Replay for Game Boy Advance,
/// made by Future Console Design (FCD).
/// </summary>
public sealed class GbaGsFcdRom : Rom
{
    private const RomClass ThisRomClass = RomClass.GbaGamesharkFcd;

    public GbaGsFcdRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomClass)
    {
    }

    public static bool Is(byte[] bytes)
    {
        // gba-cblite-r1-prototype-SST39VF100-20011019.bin
        // gba-gspro-sp-madcatz-SST39VF100-20030821.bin
        bool is128KiB = bytes.Length == 0x00020000;

        // gba-gspro-sp-karabiner-SST39VF800ATSOP48-20060614.bin
        bool is1024KiB = bytes.Length == 0x00100000;

        return (is128KiB || is1024KiB) && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        bool isMagicNumberMatch = bytes[..4].SequenceEqual(new byte[] { 0x2E, 0x00, 0x00, 0xEA });
        bool isCopyrightMatch = bytes[0x05..0x20].ToUtf8String() == "(C) Future Console Design *";
        bool isFcdFcdFcdMatch = bytes[0x80..0xA0].ToUtf8String() == "FCDFCDFCDFCDFCD!FCDFCDFCDFCDFCD!";
        return isMagicNumberMatch && isCopyrightMatch && isFcdFcdFcdMatch;
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Class == ThisRomClass;
    }

    public static bool Is(RomClass type)
    {
        return type == ThisRomClass;
    }

    public override void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"GBA FCD GameShark ROM file: '{Metadata.FilePath}'");
    }
}
