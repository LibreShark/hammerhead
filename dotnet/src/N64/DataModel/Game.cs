// bacteriamage.wordpress.com

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Represents one game and it's cheats in the GameShark's games list.
/// </summary>
public class Game
{
    public string Name { get; set; }

    public List<Cheat> Cheats { get; private set; }

    public Game()
    {
        Name = "";
        Cheats = new List<Cheat>();
    }

    public Game(string name)
        : this()
    {
        Name = name;
    }

    public Game(string name, IEnumerable<Cheat> cheats)
        : this(name)
    {
        Cheats.AddRange(cheats);
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

    public override bool Equals(object? obj)
    {
        return Equals(obj as Game);
    }

    public bool Equals(Game? game)
    {
        return string.Equals(Name, game?.Name);
    }

    public override int GetHashCode()
    {
        return (Name?.GetHashCode() ?? 0) ^ unchecked((int)0x80215bfd);
    }

    public override string ToString()
    {
        return string.Concat(Name ?? "", $" ({Cheats.Count})").Trim();
    }
}
