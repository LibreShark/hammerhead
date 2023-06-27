using System.Text.RegularExpressions;

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

public abstract class CheatDb
{
    protected readonly u8[] Bytes;

    public readonly string FilePath;
    public readonly GameConsole Console;
    public readonly IoFormat Format;

    protected CheatDb(string filePath, u8[] rawInput, GameConsole console, IoFormat format)
    {
        Bytes = rawInput;
        FilePath = filePath;
        Console = console;
        Format = format;
    }

    public bool IsKnown()
    {
        return Format != IoFormat.UnknownIoFormat;
    }

    public bool IsUnknown()
    {
        return Format == IoFormat.UnknownIoFormat;
    }

    public abstract List<Game> ReadGames();

    public abstract void WriteGames(IEnumerable<Game> games);

    public static CheatDb FromFile(string filePath)
    {
        u8[] bytes = File.ReadAllBytes(filePath);

        if (N64DatelTextDb.Is(bytes))
        {
            return new N64DatelTextDb(filePath, bytes);
        }

        return new UnknownCheatDb(filePath, bytes);
    }

    protected static string[] GetAllNonEmptyLines(u8[] buffer)
    {
        string[] lines = buffer.SplitLines();
        return lines
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }
}
