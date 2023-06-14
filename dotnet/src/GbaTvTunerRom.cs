namespace LibreShark.Hammerhead;

public class GbaTvTunerRom : Rom
{
    private const RomType ThisRomType = RomType.GbaTvTuner;

    public GbaTvTunerRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
        IsEncrypted = false;
        IsScrambled = false;
    }

    public static bool Is(byte[] bytes)
    {
        bool is512KiB = bytes.Length == 0x00080000;
        bool is16MiB = bytes.Length == 0x01000000;
        return (is512KiB || is16MiB) && DetectUnobfuscated(bytes);
    }

    private static bool DetectUnobfuscated(byte[] bytes)
    {
        var b = bytes[0xA0..0xAB];
        var s = b.ToUtf8String();
        return s == "GBA_Capture";
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
        Console.WriteLine($"GBA Blaze/Pelican TV Tuner ROM file: '{FilePath}'");
    }
}
