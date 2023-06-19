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

public interface IBinReader
{
    public u32 Position { get; }
    public bool EndReached { get; }

    public s64 Find(string needle);
    public s64 Find(byte[] needle);

    public bool Contains(string needle);
    public bool Contains(byte[] needle);

    public IBinReader Seek(u32 addr);

    public byte[] PeekBytesAt(u32 addr, u32 count);
    public byte[] PeekBytes(u32 count);

    public byte[] ReadBytesAt(u32 addr, u32 count);
    public byte[] ReadBytes(u32 count);

    public u8 ReadUByte(u32 addr);
    public u8 ReadUByte();

    public s8 ReadSByte(u32 addr);
    public s8 ReadSByte();

    public u16 ReadUInt16(u32 addr);
    public u16 ReadUInt16();

    public s16 ReadSInt16(u32 addr);
    public s16 ReadSInt16();

    public u32 ReadUInt32(u32 addr);
    public u32 ReadUInt32();

    public s32 ReadSInt32(u32 addr);
    public s32 ReadSInt32();

    public RomString ReadCString(u32 maxLen = 0);
    public RomString ReadCStringAt(u32 addr, u32 maxLen = 0);

    public RomString ReadPrintableCString(u32 maxLen = 0);
    public RomString ReadPrintableCStringAt(u32 addr, u32 maxLen = 0);
}
