// bacteriamage.wordpress.com

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Represents a single GameShark code.
/// </summary>
public class Code
{
    public uint Address { get; set; }

    public int Value { get; set; }

    public Code(uint address, int value)
    {
        Address = address;
        Value = value;
    }

    public Code()
    {
        Address = 0x80000000;
        Value = 0x0000;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Code);
    }

    public bool Equals(Code? code)
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
