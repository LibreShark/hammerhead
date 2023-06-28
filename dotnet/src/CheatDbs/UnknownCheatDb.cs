namespace LibreShark.Hammerhead.CheatDbs;

// ReSharper disable BuiltInTypeReferenceStyle
using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using s64 = Int64;
using u64 = UInt64;
using f64 = Double;

public sealed class UnknownCheatDb : CheatDb
{
    private const GameConsole ThisConsole = GameConsole.UnknownGameConsole;
    private const FileFormat ThisFileFormat = FileFormat.UnknownFileFormat;
    private const RomFormat ThisRomFormat = RomFormat.UnknownRomFormat;

    public UnknownCheatDb(string filePath, u8[] rawInput)
        : base(filePath, rawInput, ThisConsole, ThisFileFormat, ThisRomFormat)
    {
    }

    protected override List<Game> ReadGames()
    {
        return new List<Game>();
    }

    protected override u8[] WriteGames(IEnumerable<Game> games)
    {
        return Array.Empty<u8>();
    }
}
