using System.Collections.Immutable;
using System.Text.RegularExpressions;
using BetterConsoles.Tables;
using LibreShark.Hammerhead.IO;

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

public abstract class CheatDb : IDataSource
{
    public ImmutableArray<byte> RawInput { get; }
    public u8[] Buffer { get; }
    public RomMetadata Metadata { get; }
    public List<Game> Games { get; }

    public readonly FileFormat FileFormat;

    public RomFormat RomFormat => Metadata.Format;

    protected CheatDb(string filePath, u8[] rawInput, GameConsole console, FileFormat fileFormat, RomFormat romFormat)
    {
        RawInput = rawInput.ToImmutableArray();
        Buffer = rawInput;
        FileFormat = fileFormat;
        Games = new List<Game>();
        Metadata = new RomMetadata()
        {
            FilePath = filePath,
            Console = console,
            Format = romFormat,
            FileChecksum = RawInput.ComputeChecksums(),
        };
    }

    protected abstract List<Game> ReadGames();

    protected abstract u8[] WriteGames(IEnumerable<Game> games);

    public bool IsValidFormat()
    {
        return FileFormat != FileFormat.UnknownFileFormat &&
               RomFormat != RomFormat.UnknownRomFormat;
    }

    public bool IsInvalidFormat()
    {
        return FileFormat == FileFormat.UnknownFileFormat &&
               RomFormat != RomFormat.UnknownRomFormat;
    }

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

    public virtual void PrintSummary(InfoCmdParams @params)
    {
        throw new NotImplementedException();
    }
    public virtual void PrintCustomHeader(TerminalPrinter printer, InfoCmdParams @params)
    {
    }

    public virtual void PrintGames(TerminalPrinter printer, InfoCmdParams @params)
    {
        printer.PrintGames(@params);
    }

    public virtual void PrintCustomBody(TerminalPrinter printer, InfoCmdParams @params)
    {
    }

    public virtual void AddFileProps(Table table)
    {
    }
}
