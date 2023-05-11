// bacteriamage.wordpress.com

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
