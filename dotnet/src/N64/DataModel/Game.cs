// bacteriamage.wordpress.com

using System.Text.RegularExpressions;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Represents one game and it's cheats in the GameShark's games list.
/// </summary>
public class Game
{
    public string Name { get; set; }

    public List<Cheat> Cheats { get; set; }

    public Game(string name = "", IEnumerable<Cheat>? cheats = null)
    {
        Name = name;
        Cheats = new List<Cheat>(cheats ?? Array.Empty<Cheat>());
    }

    public static Game NewGame(string name)
    {
        return new Game(name);
    }

    public Cheat AddCheat(string name)
    {
        return AddCheat(new Cheat(name));
    }

    public Cheat AddCheat(Cheat cheat)
    {
        Cheats.Add(cheat);
        return cheat;
    }

    public string[] GetWarnings()
    {
        List<string> warnings = new List<string>();
        if (IsAllUpperCase(Name) && !IsAllowListed(Name))
        {
            warnings.Add("Custom user-entered game name");
        }
        if (LooksLikeACode(Name))
        {
            warnings.Add("Game name looks like a GS code");
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
        var allowlist = new string[]
        {
            "BUST-A-MOVE 2",
            "FIFA 99",
            "GT 64",
            "MK4",
            "NASCAR '99",
            "NHL '99",
            "S.C.A.R.S.",
        };
        return allowlist.Any(goodName => name == goodName);
    }

    private static bool LooksLikeACode(string name)
    {
        name = name.Trim();
        return Regex.IsMatch(name, @"[0-9a-f]{8}.?[0-9a-f]{4}", RegexOptions.IgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is Game game && Equals(game);
    }

    public bool Equals(Game? game)
    {
        return string.Equals(Name, game?.Name);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode() ^ unchecked((int)0x80215bfd);
    }

    public override string ToString()
    {
        return string.Concat(Name, $" ({Cheats.Count})").Trim();
    }
}
