namespace LibreShark.Hammerhead;

public class GbcCodeBreakerRom : Rom
{
    private const RomType ThisRomType = RomType.GbcCodebreaker;

    public GbcCodeBreakerRom(string filePath, byte[] bytes)
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
        var s = bytes[0x00..0x10].ToUtf8String();
        return s == "CodeBreaker / GB";
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
        Console.WriteLine($"GBC Code Breaker ROM file: '{FilePath}'");
    }
}
