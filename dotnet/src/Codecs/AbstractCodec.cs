using System.Collections.Immutable;
using BetterConsoles.Tables;
using Google.Protobuf;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Codecs;

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

internal class CodecFactory
{
    public Func<byte[], bool> IsMatch { get; }
    public Func<AbstractCodec> Create { get; }

    public CodecFactory(Func<u8[], bool> isMatch, Func<AbstractCodec> create)
    {
        IsMatch = isMatch;
        Create = create;
    }
}

public abstract class AbstractCodec
{
    public ImmutableArray<u8> RawInput { get; }

    /// <summary>
    /// Plain, unencrypted, unobfuscated copy of the internal ROM bytes.
    /// If the input file is encrypted/scrambled, it must be
    /// decrypted/unscrambled immediately in the subclass constructor.
    /// </summary>
    public byte[] Buffer => Scribe.GetBufferCopy();

    public VgeMetadata Metadata { get; }

    public List<Game> Games { get; }

    protected readonly AbstractBinaryScribe Scribe;

    protected AbstractCodec(
        string filePath,
        IEnumerable<byte> rawInput,
        AbstractBinaryScribe scribe,
        ConsoleId consoleId,
        CodecId codecId
    )
    {
        Scribe = scribe;
        RawInput = rawInput.ToImmutableArray();
        Games = new List<Game>();
        Metadata = new VgeMetadata
        {
            FilePath = filePath,
            ConsoleId = consoleId,
            CodecId = codecId,
            FileChecksum = RawInput.ComputeChecksums(),
        };
    }

    public virtual void PrintCustomHeader(TerminalPrinter printer, InfoCmdParams @params) {}

    public virtual void PrintGames(TerminalPrinter printer, InfoCmdParams @params) {
        if (FormatSupportsCustomCheatCodes() && !@params.HideGames)
        {
            printer.PrintGames(@params);
            Console.WriteLine();
        }
    }

    public virtual void PrintCustomBody(TerminalPrinter printer, InfoCmdParams @params) {}

    public void AddFileProps(Table table)
    {
        if (FormatSupportsFileEncryption())
        {
            table.AddRow("File encrypted", IsFileEncrypted());
        }

        if (FormatSupportsFileScrambling())
        {
            table.AddRow("File scrambled", IsFileScrambled());
        }

        if (FormatSupportsFirmwareCompression())
        {
            table.AddRow("Firmware compressed", IsFirmwareCompressed());
        }

        if (FormatSupportsUserPrefs())
        {
            table.AddRow("Pristine user prefs", !HasUserPrefs());
        }
    }

    public virtual bool FormatSupportsCustomCheatCodes()
    {
        return true;
    }

    public virtual bool FormatSupportsFileEncryption()
    {
        return false;
    }

    public virtual bool FormatSupportsFileScrambling()
    {
        return false;
    }

    public virtual bool FormatSupportsFirmwareCompression()
    {
        return false;
    }

    public virtual bool FormatSupportsUserPrefs()
    {
        return false;
    }

    public virtual bool IsFileEncrypted()
    {
        return false;
    }

    public virtual bool IsFileScrambled()
    {
        return false;
    }

    public virtual bool IsFirmwareCompressed()
    {
        return false;
    }

    public virtual bool HasUserPrefs()
    {
        return false;
    }

    public virtual u8[] Encrypt()
    {
        return Buffer;
    }

    public u8[] Decrypt()
    {
        return Buffer;
    }

    public virtual u8[] Scramble()
    {
        return Buffer;
    }

    public u8[] Unscramble()
    {
        return Buffer;
    }

    public static AbstractCodec FromFile(string romFilePath)
    {
        u8[] bytes = File.ReadAllBytes(romFilePath);

        CodecFactory[] codecFactories =
        {
            new(GbGsRom.Is, () => new GbGsRom(romFilePath, bytes)),
            new(GbaGsDatelRom.Is, () => new GbaGsDatelRom(romFilePath, bytes)),
            new(GbaGsFcdRom.Is, () => new GbaGsFcdRom(romFilePath, bytes)),
            new(GbaTvTunerRom.Is, () => new GbaTvTunerRom(romFilePath, bytes)),
            new(GbcCbRom.Is, () => new GbcCbRom(romFilePath, bytes)),
            new(GbcGsRom.Is, () => new GbcGsRom(romFilePath, bytes)),
            new(GbcMonsterBrainRom.Is, () => new GbcMonsterBrainRom(romFilePath, bytes)),
            new(GbcSharkMxRom.Is, () => new GbcSharkMxRom(romFilePath, bytes)),
            new(GbcXpRom.Is, () => new GbcXpRom(romFilePath, bytes)),
            new(N64GsRom.Is, () => new N64GsRom(romFilePath, bytes)),
            new(N64GsText.Is, () => new N64GsText(romFilePath, bytes)),
            new(N64XpRom.Is, () => new N64XpRom(romFilePath, bytes)),
            new(_ => true, () => new UnknownCodec(romFilePath, bytes)),
        };

        return codecFactories.First(factory => factory.IsMatch(bytes)).Create();
    }

    protected static RomString EmptyRomStr()
    {
        return new RomString()
        {
            Value = "",
            Addr = new RomRange()
            {
                Length = 0,
                StartIndex = 0,
                EndIndex = 0,
                RawBytes = ByteString.Empty,
            },
        };
    }

    public bool IsValidFormat()
    {
        return Metadata.CodecId != CodecId.UnspecifiedCodecId &&
               Metadata.CodecId != CodecId.UnsupportedCodecId;
    }

    public bool IsInvalidFormat()
    {
        return !IsValidFormat();
    }
}
