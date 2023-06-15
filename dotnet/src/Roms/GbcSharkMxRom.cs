namespace LibreShark.Hammerhead;

/// <summary>
/// Shark MX email client for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcSharkMxRom : Rom
{
    private const RomType ThisRomType = RomType.GbcSharkMx;

    public GbcSharkMxRom(string filePath, byte[] bytes)
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
        return bytes.Contains("Shark MX") &&
               bytes.Contains("Datel Design LTD");
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
        Console.WriteLine($"GBC Shark MX ROM file: '{Metadata.FilePath}'");
    }
}
