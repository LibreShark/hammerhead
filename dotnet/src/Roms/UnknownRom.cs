using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Roms;

public sealed class UnknownRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.UnknownGameConsole;
    private const RomFormat ThisRomFormat = RomFormat.UnknownRomFormat;

    public UnknownRom(string filePath, byte[] bytes)
        : base(filePath, bytes, new BigEndianScribe(bytes), ThisConsole, ThisRomFormat)
    {
    }

    protected override void PrintCustomHeader()
    {
    }
}
