namespace LibreShark.Hammerhead.IO;

public class EmbeddedFile
{
    public readonly string FileName;
    public readonly u8[] RawBytes;
    public readonly u8[] UncompressedBytes;
    public readonly RomRange PositionInParentFile;

    public EmbeddedFile(string fileName, u8[] rawBytes, u8[]? uncompressedBytes = null, RomRange? range = null)
    {
        FileName = fileName;
        RawBytes = rawBytes;
        UncompressedBytes = uncompressedBytes ?? rawBytes;
        PositionInParentFile = range ?? new RomRange();
    }
}
