// bacteriamage.wordpress.com

using System.Collections.Generic;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Represents one GameShark cheat which composed of individual codes.
/// </summary>
public class Cheat
{
    public string Name { get; set; }

    public bool Active { get; set; }

    public List<Code> Codes { get; private set; }

    public Cheat()
    {
        Name = "";
        Active = false;
        Codes = new List<Code>();
    }

    public Cheat(string name)
        : this()
    {
        Name = name;
    }

    public Cheat(string name, IEnumerable<Code> codes)
        : this(name)
    {
        Codes.AddRange(codes);
    }

    public Code AddCode(uint address, int value)
    {
        return AddCode(new Code(address, value));
    }

    public Code AddCode(Code code)
    {
        Codes.Add(code);
        return code;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Cheat);
    }

    public bool Equals(Cheat? cheat)
    {
        return string.Equals(Name, cheat?.Name);
    }

    public override int GetHashCode()
    {
        return (Name?.GetHashCode() ?? 0) ^ unchecked((int)0xc90f4677);
    }

    public override string ToString()
    {
        return string.Concat(Name ?? "", Active ? "" : " [off]").Trim();
    }
}
