namespace LibreShark.Hammerhead.IO;

public struct EmbeddedFile
{
    public readonly string FileName;
    public readonly u8[] RawBytes;
    public readonly u8[] UncompressedBytes;

    public EmbeddedFile(string fileName, u8[] rawBytes, u8[]? uncompressedBytes = null)
    {
        FileName = fileName;
        RawBytes = rawBytes;
        UncompressedBytes = uncompressedBytes ?? rawBytes;
    }
}
