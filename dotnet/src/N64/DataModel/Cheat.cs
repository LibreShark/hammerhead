// bacteriamage.wordpress.com

using System.Text.RegularExpressions;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Represents one GameShark cheat which is composed of zero or more codes.
/// </summary>
public class Cheat
{
    public string Name
    {
        get => _name;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value.Length is < 1 or > 30)
            {
                throw new ArgumentOutOfRangeException(
                    $"Cheat names must be 1-30 chars in length, but '{_name}' has length {_name.Length}.");
            }
            _name = value;
        }
    }

    private string _name = "";

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

    public string[] GetWarnings()
    {
        List<string> warnings = new List<string>();
        if (IsAllUpperCase(Name) && !IsAllowListed(Name))
        {
            warnings.Add("Custom user-entered cheat name");
        }
        if (LooksLikeACode(Name))
        {
            warnings.Add("Cheat name looks like a GS code");
        }
        return warnings.ToArray();
    }

    private static bool IsAllUpperCase(string name)
    {
        name = Regex.Replace(name, @"\bP[1-4]\b", "");
        return Regex.IsMatch(name, "[A-Z]") &&
               !Regex.IsMatch(name, "[a-z]");
    }

    private static bool IsAllowListed(string name)
    {
        var upper = name.ToUpperInvariant();
        return upper.Contains("MUST BE ON") ||
               upper.Contains("(M)") ||
               upper.Equals("2XRC-P90");
    }

    private static bool LooksLikeACode(string name)
    {
        name = name.Trim();
        return Regex.IsMatch(name, @"[0-9a-f]{8}.?[0-9a-f]{4}", RegexOptions.IgnoreCase);
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
