// bacteriamage.wordpress.com

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class to binary encoded a cheat list for one game.
/// </summary>
class GameEncoder
{
    public BinaryWriter Writer { get; private set; }

    public GameEncoder(BinaryWriter writer)
    {
        Writer = writer;
    }

    public void EncodeGame(Game game)
    {
        WriteGameName(game.Name);
        WriteCheats(game.Cheats);
    }

    private void WriteGameName(string name)
    {
        ValidateName(name);
        WriteCString(name);
    }

    private void WriteCheats(List<Cheat> cheats)
    {
        Writer.WriteByte(cheats.Count);

        foreach (Cheat cheat in cheats)
        {
            WriteCheat(cheat);
        }
    }

    private void WriteCheat(Cheat cheat)
    {
        WriteCheatName(cheat.Name);

        int codes = cheat.Codes.Count;

        if (cheat.IsActive)
        {
            Writer.WriteByte(cheat.Codes.Count | 0x80);
        }
        else
        {
            Writer.WriteByte(cheat.Codes.Count);
        }

        foreach (Code code in cheat.Codes)
        {
            WriteCode(code);
        }
    }

    private void WriteCode(Code code)
    {
        Writer.WriteUInt32(code.Address);
        Writer.WriteInt16(code.Value);
    }

    private void WriteCheatName(string name)
    {
        ValidateName(name);
        EncodeCommonWords(ref name);
        WriteCString(name);
    }

    const string ValidNameCharacters = "!$%^&*()[]{}0123456789,.ABCDEFGHIJKLMNOPQRSTUVWXYZ=#/<>;-+: abcdefghijklmnopqrstuvwxyz?'";

    private void ValidateName(string name)
    {
        foreach (char c in name)
        {
            if (!ValidNameCharacters.Contains(c.ToString()))
            {
                throw new Exception($"The character '{c}' is not allowed in game or cheat names.");
            }
        }

        if (name.Length < 1)
        {
            throw new Exception("Name cannot be blank.");
        }

        if (name.Length > 30)
        {
            throw new Exception($"The name {name} is too long.");
        }
    }

    private void EncodeCommonWords(ref string name)
    {
        name = name.Replace("Key", "`F6`");
        name = name.Replace("Have ", "`F7`");
        name = name.Replace("Lives", "`F8`");
        name = name.Replace("Energy", "`F9`");
        name = name.Replace("Health", "`FA`");
        name = name.Replace("Activate ", "`FB`");
        name = name.Replace("Unlimited ", "`FC`");
        name = name.Replace("Player ", "`FD`");
        name = name.Replace("Always ", "`FE`");
        name = name.Replace("Infinite ", "`FF`");
    }

    private void WriteCString(string s)
    {
        Writer.WriteCString(s);
    }
}
