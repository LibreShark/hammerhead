// bacteriamage.wordpress.com

using System.Text;
using Google.Protobuf;

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
/// Helper class for reading (big-endian) integers and c-style strings from byte buffers.
///
/// TODO(CheatoBaggins): Add transformer lambdas for replacing GS chars (e.g., "Infinite ")
/// </summary>
internal class BigEndianScribe : BinaryScribe
{
    public BigEndianScribe(byte[] buffer) : base(buffer)
    {
    }

    public override u16 ReadU16()
    {
        byte hi = ReadU8();
        byte lo = ReadU8();
        s32 value = (hi << 8) + lo;
        return (u16) value;
    }

    public override BinaryScribe WriteU16(u16 value)
    {
        byte hi = (byte)((value >> 8) & 0xFF);
        byte lo = (byte)(value & 0xFF);
        Buffer[Position++] = hi;
        Buffer[Position++] = lo;
        return this;
    }

    public override u32 ReadU32()
    {
        u32 hi = ReadU16();
        u32 lo = ReadU16();

        return (hi << 16) + lo;
    }

    public override BinaryScribe WriteU32(u32 value)
    {
        WriteU16((u16)(value >> 16));
        WriteU16((u16)(value & 0xFFFF));
        return this;
    }
}
