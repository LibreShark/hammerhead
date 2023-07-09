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

public struct EmbeddedImage
{
    public readonly string FileName;
    public readonly Image<Rgba32> Image;

    public EmbeddedImage(string fileName, Image<Rgba32> image)
    {
        FileName = fileName;
        Image = image;
    }
}
