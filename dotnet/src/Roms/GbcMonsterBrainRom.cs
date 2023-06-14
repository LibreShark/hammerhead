namespace LibreShark.Hammerhead;

public class GbcMonsterBrainRom : Rom
{
    private const RomType ThisRomType = RomType.GbcMonsterbrain;

    public GbcMonsterBrainRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
        IsEncrypted = DetectEncrypted(bytes);

        // Not applicable to GameShark ROMs. Only Xplorer 64 scrambles bytes.
        IsScrambled = false;
    }

    private static bool DetectEncrypted(byte[] bytes)
    {
        // TODO(CheatoBaggins): Implement
        return false;
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        bool is512KiB = bytes.Length == 0x00080000;
        string identifier = bytes[0x00..0x20].ToUtf8String();
        return (is256KiB || is512KiB) &&
               (identifier.StartsWith("BrainBoy") ||
                identifier.StartsWith("Monster Brain"));
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
        Console.WriteLine($"GBC Monster Brain ROM file: '{FilePath}'");
    }
}
