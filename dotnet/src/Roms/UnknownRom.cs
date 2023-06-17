namespace LibreShark.Hammerhead;

public sealed class UnknownRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.UnknownRomFormat;

    public UnknownRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
    }

    protected override void PrintCustomHeader()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"UNKNOWN ROM file with length = 0x{Bytes.Length:X8} ({Bytes.Length}): '{Metadata.FilePath}'");
    }
}
