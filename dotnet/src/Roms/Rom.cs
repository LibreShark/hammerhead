using System.Collections.Immutable;

namespace LibreShark.Hammerhead;

public abstract class Rom
{
    /// <summary>
    /// Raw, unaltered bytes that were read in from the ROM file.
    /// May be encrypted or scrambled, depending on the device.
    /// </summary>
    protected readonly ImmutableArray<byte> InitialBytes;

    /// <summary>
    /// Plain, unencrypted, unobfuscated bytes.
    /// If the input file is encrypted/scrambled, it must be
    /// decrypted/unscrambled immediately in the subclass constructor.
    /// </summary>
    protected readonly byte[] Bytes;

    public readonly RomMetadata Metadata;

    protected Rom(string filePath, byte[] bytes, RomType type)
    {
        InitialBytes = bytes.ToImmutableArray();
        Bytes = bytes.ToArray();
        Metadata = new RomMetadata
        {
            FilePath = filePath,
            Type = type,
        };
    }

    public abstract void PrintSummary();

    public virtual bool IsEncrypted() { return false; }
    public virtual bool IsScrambled() { return false; }
    public virtual bool IsCompressed() { return false; }

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
}
