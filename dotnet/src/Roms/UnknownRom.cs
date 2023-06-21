namespace LibreShark.Hammerhead.Roms;

public sealed class UnknownRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.UnknownGameConsole;
    private const RomFormat ThisRomFormat = RomFormat.UnknownRomFormat;

    public UnknownRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisConsole, ThisRomFormat)
    {
    }

    protected override void PrintCustomHeader()
    {
    }
}
