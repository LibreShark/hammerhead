namespace LibreShark.Hammerhead;

public class UnknownRom : Rom
{
    public UnknownRom(string filePath, byte[] bytes)
        : base(filePath, bytes, RomType.UnknownRomType)
    {
    }

    public override void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"UNKNOWN ROM file: '{FilePath}'");
    }
}
