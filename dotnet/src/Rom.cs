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
}
