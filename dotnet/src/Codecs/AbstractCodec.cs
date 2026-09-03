using System.Collections.Immutable;
using Google.Protobuf.Collections;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Cli;
using LibreShark.Hammerhead.GameBoy;
using LibreShark.Hammerhead.GameBoyAdvance;
using LibreShark.Hammerhead.GameBoyColor;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.Nintendo64;
using Spectre.Console;

namespace LibreShark.Hammerhead.Codecs;

public abstract class AbstractCodec : ICodec
{
    private static readonly CodecFileFactory[] CodecFactories = new[] {
        // Game Boy (original and Pocket)
        GbGsRom.Factory,

        // Game Boy Color
        GbcCbRom.Factory,
        GbcGsV3Rom.Factory,
        GbcGsV4Rom.Factory,
        GbcMonsterBrainRom.Factory,
        GbcSharkMxRom.Factory,
        GbcXpRom.Factory,

        // Game Boy Advance
        GbaGsDatelRom.Factory,
        GbaGsFcdRom.Factory,
        GbaTvTunerRom.Factory,

        // Nintendo 64
        N64EdX7Text.Factory,
        N64GbHunterRom.Factory,
        N64GsRom.Factory,
        N64GsText.Factory,
        N64XpRom.Factory,
        N64XpText.Factory,

        // Generic
        ProtobufJson.Factory,
        UnknownCodec.Factory,

        // These need to come last because they have the least-precise
        // auto-detection.
        GbcGsV3CodeFile.Factory,
        GbcGsV3CodeDb.Factory,
    };

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

    public FileMetadata Metadata => Parsed.Metadata;

    public RepeatedField<Game> Games => Parsed.Games;

    public CodecFeatureSupport Support => Metadata.CodecFeatureSupport;

    public abstract CodecId DefaultCheatOutputCodec { get; }

    public List<EmbeddedFile> EmbeddedFiles { get; }
    public List<EmbeddedImage> EmbeddedImages { get; }

    protected ParsedFile Parsed { get; private set; }

    protected readonly AbstractBinaryScribe Scribe;

    protected ICliPrinter Printer;

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
        Parsed = new ParsedFile()
        {
            Metadata = new FileMetadata()
            {
                FilePath = filePath,
                ConsoleId = consoleId,
                CodecId = codecId,
                FileChecksum = RawInput.ComputeChecksums(),
                CodecFeatureSupport = new CodecFeatureSupport(),
            },
        };
        Printer = new TerminalPrinter(this);
        EmbeddedFiles = new List<EmbeddedFile>();
        EmbeddedImages = new List<EmbeddedImage>();
    }

    protected virtual void SanitizeCustomProtoFields(ParsedFile parsed)
    {
    }

    public ICodec ImportFromProto(ParsedFile parsed)
    {
        var old = Parsed;
        Parsed = new ParsedFile(parsed)
        {
            Metadata =
            {
                CodecId = old.Metadata.CodecId,
                CodecFeatureSupport = old.Metadata.CodecFeatureSupport,
                FilePath = old.Metadata.FilePath,
            },
        };
        WriteChangesToBuffer();
        Parsed.Metadata.FileChecksum = Buffer.ComputeChecksums();
        return this;
    }

    public ParsedFile ToFullProto()
    {
        var proto = new ParsedFile(Parsed);
        Format(proto);
        return proto;
    }

    public ParsedFile ToSlimProto()
    {
        var proto = new ParsedFile(Parsed);
        SanitizeStandardProtoFields(proto);
        SanitizeCustomProtoFields(proto);
        Format(proto);
        return proto;
    }

    public virtual void RecalculateKeyCodes(N64KeyCodeId[]? newKeyCodeIds = null)
    {
        throw new NotImplementedException(
            $"'{Metadata.CodecId.ToDisplayString()}' files do not support " +
            "N64 GameShark key codes.");
    }

    public virtual void PrintCustomHeader(ICliPrinter printer, InfoCmdParams @params) {}

    public virtual void PrintGames(ICliPrinter printer, InfoCmdParams @params) {
        if (SupportsCheats() && !@params.HideGames)
        {
            printer.PrintGames(@params);
        }
    }

    public virtual void PrintCustomBody(ICliPrinter printer, InfoCmdParams @params) {}

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

    public bool SupportsFileExtraction()
    {
        return Metadata.CodecFeatureSupport.SupportsFileExtraction;
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

    public virtual u8[] Unscramble()
    {
        return Buffer;
    }

    public static ICodec ReadFromFile(string romFilePath, CodecId codecId = CodecId.Auto)
    {
        u8[] bytes = File.ReadAllBytes(romFilePath);

        CodecFileFactory? factory =
            CodecFactories.FirstOrDefault(factory => factory.IsCodec(codecId)) ??
            CodecFactories.FirstOrDefault(factory => factory.AutoDetect(bytes));

        if (factory == null)
        {
            throw new NotImplementedException($"ERROR: Unable to find codec factory for codec ID {codecId} ({codecId.ToDisplayString()}).");
        }

        ICodec codec = factory.Create(romFilePath, bytes);
        foreach (Game game in codec.Games)
        {
            foreach (Cheat cheat in game.Cheats)
            {
                foreach (Code code in cheat.Codes)
                {
                    code.Formatted = code.Bytes.ToCodeString(codec.Metadata.ConsoleId);
                }
            }
        }
        return codec;
    }

    public static ICodec CreateFromId(string outputFilePath, CodecId codecId)
    {
        u8[] bytes = Array.Empty<byte>();
        CodecFileFactory? factory = CodecFactories.FirstOrDefault(factory => factory.IsCodec(codecId));
        if (factory == null)
        {
            throw new NotImplementedException($"ERROR: Unable to find codec factory for codec ID {codecId} ({codecId.ToDisplayString()}).");
        }
        return factory.Create(outputFilePath, bytes);
    }

    public abstract ICodec WriteChangesToBuffer();

    private static void Format(ParsedFile parsedFile)
    {
        foreach (Game game in parsedFile.Games)
        {
            foreach (Cheat cheat in game.Cheats)
            {
                foreach (Code code in cheat.Codes)
                {
                    code.Formatted = code.Bytes.ToCodeString(parsedFile.Metadata.ConsoleId);
                }
            }
        }
    }

    private static void SanitizeStandardProtoFields(ParsedFile parsedFile)
    {
        RomString[] ids = parsedFile.Metadata.Identifiers
            .Select(rs => rs.WithoutAddress())
            .ToArray();
        parsedFile.Metadata.Identifiers.Clear();
        parsedFile.Metadata.Identifiers.AddRange(ids);
        var games = parsedFile.Games.Select(game =>
        {
            game.GameName = game.GameName.WithoutAddress();
            var cheats = game.Cheats.Select(cheat =>
            {
                cheat.CheatName = cheat.CheatName.WithoutAddress();
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
