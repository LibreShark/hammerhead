// bacteriamage.wordpress.com

using System.Collections.Generic;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Represents one GameShark cheat which is composed of zero or more codes.
/// </summary>
public class Cheat
{
    public string Name { get; set; }

    /// <summary>
    /// Indicates whether this cheat is enabled by default when the user boots up the GameShark and selects this game.
    /// </summary>
    public bool IsActiveByDefault { get; set; }

    public List<Code> Codes { get; private set; }

    public Cheat(string name = "", IEnumerable<Code>? codes = null)
    {
        IsActiveByDefault = false;
        Name = name;
        Codes = new List<Code>(codes ?? Array.Empty<Code>());
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
        return obj is Cheat cheat && Equals(cheat);
    }

    public bool Equals(Cheat? cheat)
    {
        return string.Equals(Name, cheat?.Name);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode() ^ unchecked((int)0xc90f4677);
    }

    public override string ToString()
    {
        return string.Concat(Name, IsActiveByDefault ? "" : " [off]").Trim();
    }
}
