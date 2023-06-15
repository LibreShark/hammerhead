namespace LibreShark.Hammerhead;

/// <summary>
/// Code Breaker for Game Boy Color and Game Boy Pocket,
/// made by Future Console Design (FCD) and Pelican Accessories.
/// </summary>
public sealed class GbcCbRom : Rom
{
    private const RomType ThisRomType = RomType.GbcCodebreaker;

    public GbcCbRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        return is256KiB && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        var identifier = bytes[0x00..0x10].ToUtf8String();
        return identifier == "CodeBreaker / GB";
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
        Console.WriteLine($"GBC Code Breaker ROM file: '{Metadata.FilePath}'");
    }
}
