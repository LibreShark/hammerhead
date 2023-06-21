// bacteriamage.wordpress.com

namespace LibreShark.Hammerhead.N64;

// ReSharper disable BuiltInTypeReferenceStyle

using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using f64 = Double;

/// <summary>
/// Represents a single GameShark code.
/// </summary>
public class N64Code
{
    public byte[] Address { get; set; }

    public byte[] Value { get; set; }

    public N64Code(byte[] address, byte[] value)
    {
        Address = address;
        Value = value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as N64Code);
    }

    public bool Equals(N64Code? code)
    {
        return (code != null) && (Address == code.Address) && (Value == code.Value);
    }

    public override int GetHashCode()
    {
        return (Address.GetHashCode() + Value.GetHashCode()).GetHashCode();
    }

    public override string ToString()
    {
        return $"{Address:X8} {Value:X4}";
    }
}
