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

        cheat.IsActive = cheatOn;

        for (int code = 0; code < codes; code++)
        {
            ReadCode(cheat);
        }
    }

    private void ReadCode(Cheat cheat)
    {
        byte[] address = Reader.ReadBytes(4);
        byte[] value = Reader.ReadBytes(2);

        cheat.AddCode(address, value);
    }

    private string ReadName()
    {
        int pos = Reader.Position;

        // Firmware does not support names longer than 30 chars.
        string name = Reader.ReadCString(pos, 30);

        if (name.Length < 1)
        {
            Console.Error.WriteLine($"WARNING at offset 0x{pos:X8}: Game and Cheat names should contain at least 1 character.");
            return name;
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
