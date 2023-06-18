// bacteriamage.wordpress.com

using System.Text.RegularExpressions;

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
/// Represents one GameShark cheat which is composed of zero or more codes.
/// </summary>
public class Cheat
{
    public string Name
    {
        get => _name;
        set
        {
            // TODO(CheatoBaggins): This only applies to N64 GameShark cheat names.
            // if (value.Length is < 1 or > 30)
            // {
            //     Console.Error.WriteLine(
            //         $"WARNING: Cheat names must be 1-30 chars in length, but '{_name}' has length {_name.Length}.");
            // }
            _name = value;
        }
    }

    private string _name = "";

    /// <summary>
    /// Indicates whether this cheat is enabled by default when the user boots up the GameShark and selects this game.
    /// </summary>
    public bool IsActive { get; set; }

    public List<Code> Codes { get; private set; }

    public Cheat(string name = "", IEnumerable<Code>? codes = null)
    {
        IsActive = false;
        Name = name;
        Codes = new List<Code>(codes ?? Array.Empty<Code>());
    }

    public Code AddCode(byte[] address, byte[] value)
    {
        return AddCode(new Code(address, value));
    }

    public Code AddCode(Code code)
    {
        Codes.Add(code);
        return code;
    }

    public Cheat AddCodes(IEnumerable<Code> codes)
    {
        Codes.AddRange(codes);
        return this;
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
        return
            // This is only here for formatting purposes, to make it easier to duplicate lines below
            // without worrying about the leading "OR".
            false
            // All brands
            || upper.Contains("(M)")
            || upper.Contains("2XRC-P90")
            || upper.Contains("MUST BE ON")
            // Equalizer (UK) and Game Buster (DE)
            || upper.Contains("ALTERNATE VERSION")
            || upper.Contains("CODES FOR ANIMALS YOU NEED")
            || upper.Contains("DO NOT TURN ALL THESE ON")
            || upper.Contains("FOR ANIMALS YOU NEED")
            || upper.Contains("FOR LEVEL SELECT")
            || upper.Contains("MUST HAVE CHEAT MENU ACTIVE")
            || upper.Contains("ONLY USE 1 OF THESE")
            || upper.Contains("ONLY USE INFINITE")
            || upper.Contains("USE ONLY 1 OF THESE")
            ;
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
        return string.Concat(Name, IsActive ? "" : " [off]").Trim();
    }
}
