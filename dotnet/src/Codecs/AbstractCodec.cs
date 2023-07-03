using System.Collections.Immutable;
using Google.Protobuf;
using LibreShark.Hammerhead.IO;
using Spectre.Console;

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

public class CodecFileFactory
{
    public Func<u8[], bool> AutoDetect { get; }
    public Func<CodecId, bool> IsCodec { get; }
    public Func<string, u8[], AbstractCodec> Create { get; }

    public CodecFileFactory(
        Func<u8[], bool> autoDetect,
        Func<CodecId, bool> isCodec,
        Func<string, u8[], AbstractCodec> create
    )
    {
        AutoDetect = autoDetect;
        IsCodec = isCodec;
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
    public u8[] Buffer
    {
        get => Scribe.GetBufferCopy();
        protected set => Scribe.ResetBuffer(value);
    }

    public FileMetadata Metadata { get; protected init; }

    public List<Game> Games { get; protected init; }

    protected readonly AbstractBinaryScribe Scribe;

    private static readonly CodecFileFactory[] CodecFactories = new[] {
        GboGsRom.Factory,
        GbaGsDatelRom.Factory,
        GbaGsFcdRom.Factory,
        GbaTvTunerRom.Factory,
        GbcCbRom.Factory,
        GbcGsV3Rom.Factory,
        GbcGsV4Rom.Factory,
        GbcMonsterBrainRom.Factory,
        GbcSharkMxRom.Factory,
        GbcXpRom.Factory,
        N64GbHunterRom.Factory,
        N64GsRom.Factory,
        N64GsText.Factory,
        N64XpRom.Factory,
        N64XpText.Factory,
        ProtobufJson.Factory,
        UnknownCodec.Factory,
    };

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
        Metadata = new FileMetadata()
        {
            FilePath = filePath,
            ConsoleId = consoleId,
            CodecId = codecId,
            FileChecksum = RawInput.ComputeChecksums(),
            CodecFeatureSupport = new CodecFeatureSupport(),
        };
    }

    protected virtual ParsedFile ToProtoImpl()
    {
        return new ParsedFile();
    }

    public ParsedFile ToProto()
    {
        var parsed = new ParsedFile(ToProtoImpl())
        {
            Metadata = Metadata,
        };
        parsed.Games.AddRange(Games);
        return NormalizeProto(parsed);
    }

    public virtual void PrintCustomHeader(TerminalPrinter printer, InfoCmdParams @params) {}

    public virtual void PrintGames(TerminalPrinter printer, InfoCmdParams @params) {
        if (SupportsCheats() && !@params.HideGames)
        {
            printer.PrintGames(@params);
        }
    }

    public virtual void PrintCustomBody(TerminalPrinter printer, InfoCmdParams @params) {}

    public void AddFileProps(Table table)
    {
        if (SupportsFileEncryption())
        {
            table.AddRow("File encrypted", $"{IsFileEncrypted()}");
        }

        if (SupportsFileScrambling())
        {
            table.AddRow("File scrambled", $"{IsFileScrambled()}");
        }

        if (SupportsFirmwareCompression())
        {
            table.AddRow("Firmware compressed", $"{IsFirmwareCompressed()}");
        }

        if (SupportsUserPrefs())
        {
            table.AddRow("Pristine user prefs", $"{HasPristineUserPrefs()}");
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

    public bool HasPristineUserPrefs()
    {
        return Metadata.CodecFeatureSupport.HasPristineUserPrefs;
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

    public static AbstractCodec ReadFromFile(string romFilePath, CodecId codecId = CodecId.Auto)
    {
        u8[] bytes = File.ReadAllBytes(romFilePath);

        CodecFileFactory? factory =
            CodecFactories.FirstOrDefault(factory => factory.IsCodec(codecId)) ??
            CodecFactories.FirstOrDefault(factory => factory.AutoDetect(bytes));

        if (factory == null)
        {
            throw new NotImplementedException($"ERROR: Unable to find codec factory for codec ID {codecId} ({codecId.ToDisplayString()}).");
        }

        return factory.Create(romFilePath, bytes);
    }

    public static AbstractCodec CreateFromId(string outputFilePath, CodecId codecId)
    {
        u8[] bytes = Array.Empty<byte>();
        CodecFileFactory? factory = CodecFactories.FirstOrDefault(factory => factory.IsCodec(codecId));
        if (factory == null)
        {
            throw new NotImplementedException($"ERROR: Unable to find codec factory for codec ID {codecId} ({codecId.ToDisplayString()}).");
        }
        return factory.Create(outputFilePath, bytes);
    }

    public abstract AbstractCodec WriteChangesToBuffer();

    private static ParsedFile NormalizeProto(ParsedFile parsedFile)
    {
        var ids = parsedFile.Metadata.Identifiers.Select(rs => rs.RemoveAddress()).ToArray();
        parsedFile.Metadata.Identifiers.Clear();
        parsedFile.Metadata.Identifiers.AddRange(ids);
        var games = parsedFile.Games.Select(game =>
        {
            game.GameName = game.GameName.RemoveAddress();
            var cheats = game.Cheats.Select(cheat =>
            {
                cheat.CheatName = cheat.CheatName.RemoveAddress();
                var codes = cheat.Codes.Select(code =>
                {
                    code.Formatted = code.Bytes.ToCodeString(parsedFile.Metadata.ConsoleId);
                    return code;
                }).ToArray();
                cheat.Codes.Clear();
                cheat.Codes.AddRange(codes);
                return cheat;
            }).ToArray();
            game.Cheats.Clear();
            game.Cheats.AddRange(cheats);
            return game;
        }).ToArray();
        parsedFile.Games.Clear();
        parsedFile.Games.AddRange(games);
        return parsedFile;
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
