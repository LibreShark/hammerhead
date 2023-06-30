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

internal class ExistingCodecFileFactory
{
    public Func<byte[], bool> IsMatch { get; }
    public Func<AbstractCodec> Create { get; }

    public ExistingCodecFileFactory(Func<u8[], bool> isMatch, Func<AbstractCodec> create)
    {
        IsMatch = isMatch;
        Create = create;
    }
}

internal class NewCodecFileFactory
{
    public Func<CodecId, bool> IsMatch { get; }
    public Func<AbstractCodec> Create { get; }

    public NewCodecFileFactory(Func<CodecId, bool> isMatch, Func<AbstractCodec> create)
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
    public byte[] Buffer
    {
        get => Scribe.GetBufferCopy();
        protected set => Scribe.ResetBuffer(value);
    }

    public VgeMetadata Metadata { get; }

    public List<Game> Games { get; }

    protected readonly AbstractBinaryScribe Scribe;

    public CodecFeatureSupport Support => Metadata.CodecFeatureSupport;

    public abstract CodecId DefaultCheatOutputCodec { get; }

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
            CodecFeatureSupport = new CodecFeatureSupport(),
        };
    }

    public virtual void PrintCustomHeader(TerminalPrinter printer, InfoCmdParams @params) {}

    public virtual void PrintGames(TerminalPrinter printer, InfoCmdParams @params) {
        if (SupportsCheats() && !@params.HideGames)
        {
            printer.PrintGames(@params);
            Console.WriteLine();
        }
    }

    public virtual void PrintCustomBody(TerminalPrinter printer, InfoCmdParams @params) {}

    public void AddFileProps(Table table)
    {
        if (SupportsFileEncryption())
        {
            table.AddRow("File encrypted", IsFileEncrypted());
        }

        if (SupportsFileScrambling())
        {
            table.AddRow("File scrambled", IsFileScrambled());
        }

        if (SupportsFirmwareCompression())
        {
            table.AddRow("Firmware compressed", IsFirmwareCompressed());
        }

        if (SupportsUserPrefs())
        {
            table.AddRow("Pristine user prefs", !HasDirtyUserPrefs());
        }
    }

    public bool SupportsCheats()
    {
        return Metadata.CodecFeatureSupport.SupportsCheats;
    }

    public bool SupportsFileEncryption()
    {
        return Metadata.CodecFeatureSupport.SupportsFileEncryption;
    }

    public bool SupportsFileScrambling()
    {
        return Metadata.CodecFeatureSupport.SupportsFileScrambling;
    }

    public bool SupportsFirmwareCompression()
    {
        return Metadata.CodecFeatureSupport.SupportsFirmwareCompression;
    }

    public bool SupportsUserPrefs()
    {
        return Metadata.CodecFeatureSupport.SupportsUserPrefs;
    }

    public bool IsFileEncrypted()
    {
        return Metadata.CodecFeatureSupport.IsFileEncrypted;
    }

    public bool IsFileScrambled()
    {
        return Metadata.CodecFeatureSupport.IsFileScrambled;
    }

    public bool IsFirmwareCompressed()
    {
        return Metadata.CodecFeatureSupport.IsFirmwareCompressed;
    }

    public bool HasDirtyUserPrefs()
    {
        return Metadata.CodecFeatureSupport.HasDirtyUserPrefs;
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

    public static AbstractCodec ReadFromFile(string romFilePath)
    {
        u8[] bytes = File.ReadAllBytes(romFilePath);

        ExistingCodecFileFactory[] codecFactories =
        {
            new(GboGsRom.Is, () => new GboGsRom(romFilePath, bytes)),
            new(GbaGsDatelRom.Is, () => new GbaGsDatelRom(romFilePath, bytes)),
            new(GbaGsFcdRom.Is, () => new GbaGsFcdRom(romFilePath, bytes)),
            new(GbaTvTunerRom.Is, () => new GbaTvTunerRom(romFilePath, bytes)),
            new(GbcCbRom.Is, () => new GbcCbRom(romFilePath, bytes)),
            new(GbcGsV3Rom.Is, () => new GbcGsV3Rom(romFilePath, bytes)),
            new(GbcGsV4Rom.Is, () => new GbcGsV4Rom(romFilePath, bytes)),
            new(GbcMonsterBrainRom.Is, () => new GbcMonsterBrainRom(romFilePath, bytes)),
            new(GbcSharkMxRom.Is, () => new GbcSharkMxRom(romFilePath, bytes)),
            new(GbcXpRom.Is, () => new GbcXpRom(romFilePath, bytes)),
            new(N64GsRom.Is, () => new N64GsRom(romFilePath, bytes)),
            new(N64GsText.Is, () => new N64GsText(romFilePath, bytes)),
            new(N64XpRom.Is, () => new N64XpRom(romFilePath, bytes)),
            new(N64XpText.Is, () => new N64XpText(romFilePath, bytes)),
            new(_ => true, () => new UnknownCodec(romFilePath, bytes)),
        };

        return codecFactories.First(factory => factory.IsMatch(bytes)).Create();
    }

    public static AbstractCodec CreateFromId(string outputFilePath, CodecId codecId)
    {
        u8[] bytes = Array.Empty<byte>();
        NewCodecFileFactory[] codecFactories =
        {
            new(N64GsText.Is, () => new N64GsText(outputFilePath, bytes)),
            new(N64XpText.Is, () => new N64XpText(outputFilePath, bytes)),
            new(_ => true, () => throw new InvalidOperationException(
                $"{codecId.ToDisplayString()} files cannot be created from scratch.")),
        };

        return codecFactories.First(factory => factory.IsMatch(codecId)).Create();
    }

    public abstract AbstractCodec WriteChangesToBuffer();

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
