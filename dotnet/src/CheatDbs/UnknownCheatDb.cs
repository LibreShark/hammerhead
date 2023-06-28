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

public class UnknownCheatDb : CheatDb
{
    private const GameConsole ThisConsole = GameConsole.UnknownGameConsole;
    private const FileFormat ThisFileFormat = FileFormat.UnknownFileFormat;

    public UnknownCheatDb(string filePath, u8[] rawInput)
        : base(filePath, rawInput, ThisConsole, ThisFileFormat)
    {
    }

    protected override List<Game> ReadGames()
    {
        return new List<Game>();
    }

    protected override void WriteGames(IEnumerable<Game> games)
    {
    }
}
