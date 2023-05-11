// bacteriamage.wordpress.com

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class to decode a binary encoded cheat list for one game.
/// </summary>
class GameDecoder
{
    public BinaryReader Reader { get; private set; }

    public static Game FromReader(BinaryReader reader)
    {
        GameDecoder encoder = new GameDecoder(reader);
        return encoder.ReadGame();
    }

    public GameDecoder(BinaryReader reader)
    {
        Reader = reader;
    }

    public Game ReadGame()
    {
        // Console.WriteLine("----------------------------------------");
        // Console.WriteLine($"Reading game at 0x{Reader.Position:X08}");
        Game game = Game.NewGame(ReadName());
        // Console.WriteLine($"Found game \"{game.Name}\"");

        int cheats = Reader.ReadUByte();
        // Console.WriteLine($"{cheats} cheat(s) expected");

        for (int cheat = 0; cheat < cheats; cheat++)
        {
            ReadCheat(game);
        }

        return game;
    }

    private void ReadCheat(Game game)
    {
        // Console.WriteLine($"Reading cheat name at 0x{Reader.Position:X08}");
        Cheat cheat = game.AddCheat(ReadName());

        int codes = Reader.ReadUByte();

        bool cheatOn = (codes & 0x80) > 0;

        codes &= 0x7F;

        cheat.IsActiveByDefault = cheatOn;

        for (int code = 0; code < codes; code++)
        {
            ReadCode(cheat);
        }
    }

    private void ReadCode(Cheat cheat)
    {
        uint address = Reader.ReadUInt32();
        int value = Reader.ReadUInt16();

        cheat.AddCode(address, value);
    }

    private string ReadName()
    {
        int pos = Reader.Position;

        string name = Reader.ReadCString(30);

        if (name.Length is < 1 or > 30)
        {
            throw new Exception($"Invalid game or cheat name: '{name}' at offset 0x{pos:X08}. Names be printable ASCII between 1-30 characters long, but found length = {name.Length}.");
        }

        name = name.Replace("`F6`", "Key");
        name = name.Replace("`F7`", "Have ");
        name = name.Replace("`F8`", "Lives");
        name = name.Replace("`F9`", "Energy");
        name = name.Replace("`FA`", "Health");
        name = name.Replace("`FB`", "Activate ");
        name = name.Replace("`FC`", "Unlimited ");
        name = name.Replace("`FD`", "Player ");
        name = name.Replace("`FE`", "Always ");
        name = name.Replace("`FF`", "Infinite ");

        return name;
    }
}
