namespace LibreShark.Hammerhead;

public sealed class UnknownRom : Rom
{
    private const RomClass ThisRomClass = RomClass.UnknownRomClass;

    public UnknownRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomClass)
    {
    }

    public override void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"UNKNOWN ROM file with length = 0x{Bytes.Length:X8} ({Bytes.Length}): '{Metadata.FilePath}'");
    }
}
