using System.Collections.Immutable;
using BetterConsoles.Tables;

namespace LibreShark.Hammerhead.IO;

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

public interface IDataSource
{
    public ImmutableArray<u8> RawInput { get; }

    /// <summary>
    /// Plain, unencrypted, unobfuscated bytes.
    /// If the input file is encrypted/scrambled, it must be
    /// decrypted/unscrambled immediately in the subclass constructor.
    /// </summary>
    public byte[] Buffer { get; }

    public RomMetadata Metadata { get; }

    public List<Game> Games { get; }

    public void PrintCustomHeader(TerminalPrinter printer, InfoCmdParams @params);

    public void PrintGames(TerminalPrinter printer, InfoCmdParams @params);

    public void PrintCustomBody(TerminalPrinter printer, InfoCmdParams @params);

    public void AddFileProps(Table table);
}
