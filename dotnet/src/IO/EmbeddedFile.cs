namespace LibreShark.Hammerhead.IO;

public class EmbeddedFile
{
    public readonly string FileName;
    public u8[] CompressedBytes;
    public u8[] UncompressedBytes;
    public readonly RomRange PositionInParentFile;

    public EmbeddedFile(string fileName, u8[] compressedBytes, u8[]? uncompressedBytes = null, RomRange? range = null)
    {
        FileName = fileName;
        CompressedBytes = compressedBytes;
        UncompressedBytes = uncompressedBytes ?? compressedBytes;
        PositionInParentFile = range ?? new RomRange();
    }
}
