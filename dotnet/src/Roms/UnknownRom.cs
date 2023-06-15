namespace LibreShark.Hammerhead;

public sealed class UnknownRom : Rom
{
    private const RomType ThisRomType = RomType.UnknownRomType;

    public UnknownRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
    }

    public override void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"UNKNOWN ROM file with length = 0x{Bytes.Length:X8} ({Bytes.Length}): '{FilePath}'");
    }
}
