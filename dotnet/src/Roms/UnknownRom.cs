namespace LibreShark.Hammerhead.Roms;

public sealed class UnknownRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.UnknownRomFormat;

    public UnknownRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
    }

    protected override void PrintCustomHeader()
    {
    }
}
