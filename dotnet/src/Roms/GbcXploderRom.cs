namespace LibreShark.Hammerhead;

public class GbcXploderRom : Rom
{
    private const RomType ThisRomType = RomType.GbcXploder;

    public GbcXploderRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
        IsEncrypted = false;
        IsScrambled = false;
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        return is256KiB && DetectUnobfuscated(bytes);
    }

    private static bool DetectUnobfuscated(byte[] bytes)
    {
        return bytes[0x00..0x0A].ToUtf8String() == "Xplorer-GB" &&
               bytes.Contains("Future Console Design!");
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
        Console.WriteLine($"GBC Xploder ROM file: '{FilePath}'");
    }
}
