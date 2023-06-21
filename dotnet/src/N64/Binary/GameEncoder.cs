// bacteriamage.wordpress.com

using System.Text.RegularExpressions;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class to binary encoded a cheat list for one game.
/// </summary>
class GameEncoder
{
    private const string ValidNameCharacters = "!$%^&*()[]{}0123456789,.ABCDEFGHIJKLMNOPQRSTUVWXYZ=#/<>;-+: abcdefghijklmnopqrstuvwxyz?'";

    private BinaryWriter Writer { get; set; }

    public N64GsVersion? Version { get; private set; }

    public GameEncoder(BinaryWriter writer, N64GsVersion? version = null)
    {
        Writer = writer;
        Version = version;
    }

    public void EncodeGame(N64Game game, int gameIndex)
    {
        var gameName = game.Name;
        SanitizeName(ref gameName);
        if (gameName.Length == 0)
        {
            Console.Error.WriteLine($"⚠️  WARNING: Skipping invalid game[{gameIndex}].Name: '{game.Name}'");
            return;
        }

        WriteGameName(game.Name);
        WriteCheats(game.Cheats, gameIndex);
    }

    private void WriteGameName(string name)
    {
        ValidateName(name);
        WriteCString(name);
    }

    private void WriteCheats(List<N64Cheat> cheats, int gameIndex)
    {
        Writer.WriteByte(cheats.Count);

        var i = 0;
        foreach (N64Cheat cheat in cheats)
        {
            WriteCheat(cheat, i, gameIndex);
            i++;
        }
    }

    private void WriteCheat(N64Cheat cheat, int cheatIndex, int gameIndex)
    {
        var cheatName = cheat.Name;
        SanitizeName(ref cheatName);
        if (cheatName.Length == 0)
        {
            Console.Error.WriteLine($"⚠️  WARNING: Skipping invalid game[{gameIndex}].cheat[{cheatIndex}].Name: '{cheat.Name}'");
            return;
        }

        WriteCheatName(cheatName);

        int activeBit = cheat.IsActive ? 0x80 : 0x00;
        Writer.WriteByte(cheat.Codes.Count | activeBit);

        foreach (N64Code code in cheat.Codes)
        {
            WriteCode(code);
        }
    }

    private void WriteCode(N64Code code)
    {
        Writer.WriteBytes(code.Address);
        Writer.WriteBytes(code.Value);
    }

    private void WriteCheatName(string name)
    {
        ValidateName(name);
        EncodeCommonWords(ref name);
        WriteCString(name);
    }

    private static void SanitizeName(ref string name)
    {
        if (LooksLikeACode(name) ||
            string.IsNullOrWhiteSpace(name))
        {
            name = "";
        }
    }

    private static bool LooksLikeACode(string name)
    {
        name = name.Trim();
        return Regex.IsMatch(name, @"[0-9a-f]{8}.?[0-9a-f]{4}", RegexOptions.IgnoreCase);
    }

    private void ValidateName(string name)
    {
        foreach (char c in name)
        {
            if (!ValidNameCharacters.Contains(c))
            {
                throw new Exception($"The character '{c}' is not allowed in game or cheat names.");
            }
        }

        switch (name.Length)
        {
            case < 1:
                Console.Error.WriteLine("WARNING: Game and Cheat names should contain at least 1 character.");
                break;
            case > 30:
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
