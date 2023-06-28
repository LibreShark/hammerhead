using System.Collections.Immutable;
using BetterConsoles.Tables;
using Google.Protobuf;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Roms;

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

public abstract class Rom : IDataSource
{
    public ImmutableArray<u8> RawInput { get; }

    /// <summary>
    /// Plain, unencrypted, unobfuscated copy of the internal ROM bytes.
    /// If the input file is encrypted/scrambled, it must be
    /// decrypted/unscrambled immediately in the subclass constructor.
    /// </summary>
    public byte[] Buffer => Scribe.GetBufferCopy();

    public RomMetadata Metadata { get; }

    public List<Game> Games { get; }

    protected readonly BinaryScribe Scribe;

    protected Rom(
        string filePath,
        IEnumerable<byte> rawInput,
        BinaryScribe scribe,
        GameConsole console,
        RomFormat format
    )
    {
        Scribe = scribe;
        RawInput = rawInput.ToImmutableArray();
        Checksum checksum = Checksum.From(RawInput);
        Games = new List<Game>();
        Metadata = new RomMetadata
        {
            FilePath = filePath,
            Console = console,
            Format = format,
            FileChecksum = new ChecksumResult()
            {
                Crc32Hex = checksum.Crc32Hex,
                Crc32CHex = checksum.Crc32CHex,
                Md5Hex = checksum.Md5Hex,
                Sha1Hex = checksum.Sha1Hex,
            },
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

    public static Rom FromFile(string romFilePath)
    {
        byte[] bytes = File.ReadAllBytes(romFilePath);

        if (N64GsRom.Is(bytes))
        {
            return new N64GsRom(romFilePath, bytes);
        }

        if (N64XpRom.Is(bytes))
        {
            return new N64XpRom(romFilePath, bytes);
        }

        if (N64GbHunterRom.Is(bytes))
        {
            return new N64GbHunterRom(romFilePath, bytes);
        }

        if (GbGsRom.Is(bytes))
        {
            return new GbGsRom(romFilePath, bytes);
        }

        if (GbcCbRom.Is(bytes))
        {
            return new GbcCbRom(romFilePath, bytes);
        }

        if (GbcXpRom.Is(bytes))
        {
            return new GbcXpRom(romFilePath, bytes);
        }

        if (GbcGsRom.Is(bytes))
        {
            return new GbcGsRom(romFilePath, bytes);
        }

        if (GbcSharkMxRom.Is(bytes))
        {
            return new GbcSharkMxRom(romFilePath, bytes);
        }

        if (GbaGsDatelRom.Is(bytes))
        {
            return new GbaGsDatelRom(romFilePath, bytes);
        }

        if (GbaGsFcdRom.Is(bytes))
        {
            return new GbaGsFcdRom(romFilePath, bytes);
        }

        if (GbaTvTunerRom.Is(bytes))
        {
            return new GbaTvTunerRom(romFilePath, bytes);
        }

        if (GbcMonsterBrainRom.Is(bytes))
        {
            return new GbcMonsterBrainRom(romFilePath, bytes);
        }

        return new UnknownRom(romFilePath, bytes);
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
        return Metadata.Format != RomFormat.UnknownRomFormat;
    }

    public bool IsInvalidFormat()
    {
        return Metadata.Format == RomFormat.UnknownRomFormat;
    }
}
