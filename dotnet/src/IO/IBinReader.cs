namespace LibreShark.Hammerhead.IO;

// ReSharper disable BuiltInTypeReferenceStyle

using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using f64 = Double;

public interface IBinReader
{
    public u32 Position { get; }
    public bool EndReached { get; }

    public s32 Find(string needle);
    public s32 Find(byte[] needle);

    public bool Contains(string needle);
    public bool Contains(byte[] needle);

    public IBinReader Seek(uint addr);

    public byte[] PeekBytesAt(uint addr, uint count);
    public byte[] PeekBytes(uint count);

    public byte[] ReadBytesAt(uint addr, uint count);
    public byte[] ReadBytes(uint count);

    public u8 ReadUByte(uint addr);
    public u8 ReadUByte();

    public s8 ReadSByte(uint addr);
    public s8 ReadSByte();

    public u16 ReadUInt16(uint addr);
    public u16 ReadUInt16();

    public s16 ReadSInt16(uint addr);
    public s16 ReadSInt16();

    public u32 ReadUInt32(uint addr);
    public u32 ReadUInt32();

    public s32 ReadSInt32(uint addr);
    public s32 ReadSInt32();

    public RomString ReadCString(int maxLen = 0);
    public RomString ReadCStringAt(uint addr, int maxLen = 0);

    public RomString ReadPrintableCString(int maxLen = 0);
    public RomString ReadPrintableCStringAt(uint addr, int maxLen = 0);
}
