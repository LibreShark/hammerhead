﻿// bacteriamage.wordpress.com

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class to binary encoded a cheat list for one game.
/// </summary>
class GameEncoder
{
    private BinaryWriter Writer { get; set; }

    public RomVersion? Version { get; private set; }

    public GameEncoder(BinaryWriter writer, RomVersion? version = null)
    {
        Writer = writer;
        Version = version;
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

        if (cheat.IsActiveByDefault)
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
            throw new Exception("Names must contain at least 1 character.");
        }

        if (name.Length > 30)
        {
            throw new Exception($"Name \"{name}\" is too long (maxlen = 30, but found length = {name.Length}).");
        }
    }

    private void EncodeCommonWords(ref string name)
    {
        // v1.02 does NOT support this.
        // v1.04 DOES support this.
        // Unknown if v1.03 supports this.
        var isSupported = Version?.Number >= 1.04;
        if (!isSupported)
        {
            return;
        }

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
