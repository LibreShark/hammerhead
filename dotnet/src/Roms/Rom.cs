namespace LibreShark.Hammerhead;

public abstract class Rom
{
    public readonly string FilePath;
    public readonly byte[] Bytes;
    public readonly RomType Type;

    public bool IsEncrypted { get; protected set; }
    public bool IsScrambled { get; protected set; }

    protected Rom(string filePath, byte[] bytes, RomType type)
    {
        FilePath = filePath;
        Bytes = bytes;
        Type = type;
    }

    public abstract void PrintSummary();

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
        if (GbaFcdGsRom.Is(bytes))
        {
            return new GbaFcdGsRom(romFilePath, bytes);
        }
        if (GbaTvTunerRom.Is(bytes))
        {
            return new GbaTvTunerRom(romFilePath, bytes);
        }

        return new UnknownRom(romFilePath, bytes);
    }
}
