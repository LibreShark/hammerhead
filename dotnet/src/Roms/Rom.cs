using System.Collections.Immutable;

namespace LibreShark.Hammerhead;

public abstract class Rom
{
    protected readonly ImmutableArray<byte> InitialBytes;

    // Plain, unencrypted, unobfuscated bytes.
    // If the input file is encrypted/scrambled, it must be
    // decrypted/descrambled immediately in the subclass constructor.
    protected readonly byte[] Bytes;

    public readonly string FilePath;
    public readonly RomType Type;

    protected Rom(string filePath, byte[] bytes, RomType type)
    {
        FilePath = filePath;
        InitialBytes = bytes.ToImmutableArray();
        Bytes = bytes.ToArray();
        Type = type;
    }

    public abstract void PrintSummary();

    public virtual bool IsEncrypted() { return false; }
    public virtual bool IsScrambled() { return false; }

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
        if (GbcCodeBreakerRom.Is(bytes))
        {
            return new GbcCodeBreakerRom(romFilePath, bytes);
        }
        if (GbcXploderRom.Is(bytes))
        {
            return new GbcXploderRom(romFilePath, bytes);
        }
        if (GbcGsRom.Is(bytes))
        {
            return new GbcGsRom(romFilePath, bytes);
        }
        if (GbcSharkMxRom.Is(bytes))
        {
            return new GbcSharkMxRom(romFilePath, bytes);
        }
        if (GbaDatelGsRom.Is(bytes))
        {
            return new GbaDatelGsRom(romFilePath, bytes);
        }
        if (GbaFcdGsRom.Is(bytes))
        {
            return new GbaFcdGsRom(romFilePath, bytes);
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
