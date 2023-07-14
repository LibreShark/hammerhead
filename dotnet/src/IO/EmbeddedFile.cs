using Google.Protobuf.WellKnownTypes;
using LibreShark.Hammerhead.Nintendo64;

namespace LibreShark.Hammerhead.IO;

public class EmbeddedFile
{
    public readonly string FileName;

    public u8[] CompressedBytes { get; private set; }
    public u8[] UncompressedBytes { get; private set; }
    public readonly RomRange PositionInParentFile;

    public EmbeddedFile(string fileName, u8[] compressedBytes, u8[]? uncompressedBytes = null, RomRange? range = null)
    {
        FileName = fileName;
        CompressedBytes = compressedBytes;
        UncompressedBytes = uncompressedBytes ?? compressedBytes;
        PositionInParentFile = range ?? new RomRange();
    }

    public void Compress()
    {
        CompressedBytes = new N64GsLzariEncoder().Encode(UncompressedBytes);
    }

    public void SetUncompressedBytes(u8[] uncompressed)
    {
        UncompressedBytes = uncompressed;
        Compress();
    }
}
