namespace LibreShark.Hammerhead;

/// <summary>
/// Monster Brain and Brain Boy for Game Boy Color and Game Boy Pocket,
/// made by Future Console Design (FCD) and Pelican Accessories.
/// </summary>
public sealed class GbcMonsterBrainRom : Rom
{
    private const RomType ThisRomType = RomType.GbcMonsterbrain;

    public GbcMonsterBrainRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
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
        string identifier = bytes[0x00..0x20].ToUtf8String();
        return identifier.StartsWith("BrainBoy") ||
               identifier.StartsWith("Monster Brain");
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Type == ThisRomType;
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
        Console.WriteLine($"GBC Monster Brain ROM file: '{Metadata.FilePath}'");
    }
}
