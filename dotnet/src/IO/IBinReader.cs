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
    #region Seeking

    public u32 Position { get; }
    public bool EndReached { get; }

    public IBinReader Seek(u32 addr);

    #endregion

    #region Find / Contains

    public s64 Find(string needle);
    public s64 Find(byte[] needle);

    public bool Contains(string needle);
    public bool Contains(byte[] needle);

    #endregion

    #region Bytes

    public u8[] PeekBytesAt(u32 addr, u32 count);
    public u8[] PeekBytes(u32 count);

    public u8[] ReadBytes(u32 count);

    #endregion

    #region Integers

    public u8 ReadU8();

    public s8 ReadS8();

    public u16 ReadU16();

    public s16 ReadS16();

    public u32 ReadU32();

    public s32 ReadS32();

    #endregion

    #region Strings

    public RomString ReadCString(u32 maxLen = 0);
    public RomString ReadCStringAt(u32 addr, u32 maxLen = 0);

    public RomString ReadPrintableCString(u32 maxLen = 0);
    public RomString ReadPrintableCStringAt(u32 addr, u32 maxLen = 0);

    #endregion
}
