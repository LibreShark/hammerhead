namespace LibreShark.Hammerhead.IO;

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

public struct EmbeddedFile
{
    public string FileName;
    public u8[] RawBytes;
    public u8[] UncompressedBytes;

    public EmbeddedFile(string fileName, u8[] rawBytes, u8[]? uncompressedBytes = null)
    {
        FileName = fileName;
        RawBytes = rawBytes;
        UncompressedBytes = uncompressedBytes ?? rawBytes;
    }
}
