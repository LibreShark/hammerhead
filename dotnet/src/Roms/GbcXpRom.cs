namespace LibreShark.Hammerhead;

/// <summary>
/// Xploder GB (aka "Xplorer GB") for Game Boy Color and Game Boy Pocket,
/// made by Blaze and Future Console Design (FCD).
/// </summary>
public sealed class GbcXpRom : Rom
{
    private const RomClass ThisRomClass = RomClass.GbcXploder;

    public GbcXpRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomClass)
    {
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        return is256KiB && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        return bytes[0x00..0x0A].ToUtf8String() == "Xplorer-GB" &&
               bytes.Contains("Future Console Design!");
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
        Console.WriteLine($"GBC Xploder ROM file: '{Metadata.FilePath}'");
    }
}
