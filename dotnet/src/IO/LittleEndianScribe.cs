// bacteriamage.wordpress.com

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

/// <summary>
/// Helper class for reading (little-endian) integers and c-style strings from byte buffers.
///
/// TODO(CheatoBaggins): Add transformer lambdas for replacing GS chars (e.g., "Infinite ")
/// </summary>
internal class LittleEndianScribe : BinaryScribe
{
    public LittleEndianScribe(u8[] bufferRef) : base(bufferRef)
    {
    }

    public override u16 ReadU16()
    {
        u8 lo = ReadU8();
        u8 hi = ReadU8();
        s32 value = (hi << 8) + lo;
        return (u16) value;
    }

    public override BinaryScribe WriteU16(u16 value)
    {
        u8 hi = (u8)((value >> 8) & 0xFF);
        u8 lo = (u8)(value & 0xFF);
        BufferRef[Position++] = lo;
        BufferRef[Position++] = hi;
        return this;
    }

    public override u32 ReadU32()
    {
        u32 lo = ReadU16();
        u32 hi = ReadU16();

        return (hi << 16) + lo;
    }

    public override BinaryScribe WriteU32(u32 value)
    {
        u16 hi = (u16)(value >> 16);
        u16 lo = (u16)(value & 0xFFFF);
        WriteU16(lo);
        WriteU16(hi);
        return this;
    }
}
