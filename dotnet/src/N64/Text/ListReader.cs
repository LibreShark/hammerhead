// bacteriamage.wordpress.com

using System.Globalization;
using System.Text.RegularExpressions;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Read a list of games from a text file in the same format used by the official utility.
/// </summary>
class ListReader
{
    private IEnumerable<string> Lines;

    private readonly List<N64Game> Games = new List<N64Game>();
    private N64Game? Game;
    private N64Cheat? Cheat;
    private int LineNumber;

    public static List<N64Game> ReadLines(string path)
    {
        return ReadLines(File.ReadAllLines(path));
    }

    public static List<N64Game> ReadLines(IEnumerable<string> lines)
    {
        return new ListReader(lines).ReadLines();
    }

    private ListReader(IEnumerable<string> lines)
        : this()
    {
        Lines = lines;
    }

    private ListReader()
    {
        Lines = new List<string>();
        Game = null;
        Cheat = null;
        LineNumber = 1;
    }

    private List<N64Game> ReadLines()
    {
        foreach (string line in Lines)
        {
            ProcessLine(line);
            LineNumber++;
        }

        return Games;
    }

    private void ProcessLine(string line)
    {
        if (MatchName(line, out Match match))
        {
            ProcessNameMatch(match);
        }
        else if (MatchCode(line, out match))
        {
            ProcessCodeMatch(match);
        }
        else if (MatchEnd(line, out match))
        {
            ProcessEndMatch(match);
        }
        else if (MatchBlank(line, out match))
        {
            ProcessBlankMatch(match);
        }
        else
        {
            ProcessUnrecognized();
        }
    }

    public static byte[] StringToByteArray(String hex)
    {
        int numChars = hex.Length;
        byte[] bytes = new byte[numChars / 2];
        for (int i = 0; i < numChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }

    private static bool MatchName(string line, out Match match)
    {
        return MatchRegex(line, @"^ *""(?<name>[^""]*)"" *(?<off>\.off)? *(?<comment>;.*)?$", out match);
    }

    private void ProcessNameMatch(Match match)
    {
        string name = match.Groups["name"]?.Value ?? "";
        bool off = !string.IsNullOrEmpty(match.Groups["off"]?.Value);

        if (string.IsNullOrEmpty(name))
        {
            throw new Exception($"Name cannot be blank on line {LineNumber}.");
        }
        if (string.IsNullOrEmpty(name))
        {
            throw new Exception($"Name is too long on line {LineNumber}.");
        }
        if (Game == null && off)
        {
            throw new Exception($@""".off"" is unexpected after game name on line {LineNumber}.");
        }

        if (Game == null)
        {
            Games.Add(Game = new N64Game(name));
        }
        else
        {
            Cheat = Game.AddCheat(name);
            Cheat.IsActive = !off;
        }
    }

    private static bool MatchCode(string line, out Match match)
    {
        return MatchRegex(line, @"^ *(?<address>[0-9A-F]{8}) +(?<value>[0-9A-F]{4}) *(?<comment>;.*)?$", out match);
    }

    private void ProcessCodeMatch(Match match)
    {
        byte[] address = StringToByteArray(match.Groups["address"].Value);
        byte[] value = StringToByteArray(match.Groups["value"].Value);

        if (Cheat == null)
        {
            throw new Exception($"Code unexpected on line {LineNumber}.");
        }

        Cheat.AddCode(address, value);
    }

    private static bool MatchEnd(string line, out Match match)
    {
        return MatchRegex(line, @"^ *(?<end>\.end) *(?<comment>;.*)?$", out match);
    }

    private void ProcessEndMatch(Match match)
    {
        if (Game == null)
        {
            throw new Exception($@""".end"" unexpected on line {LineNumber}.");
        }

        Game = null;
        Cheat = null;
    }

    private static bool MatchBlank(string line, out Match match)
    {
        return MatchRegex(line, @"^ *(?<comment>;.*)?$", out match);
    }

    private void ProcessBlankMatch(Match match)
    {
    }

    private void ProcessUnrecognized()
    {
        throw new Exception($"Line {LineNumber} is not recognized.");
    }

    private static bool MatchRegex(string line, string pattern, out Match match)
    {
        match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
        return match?.Success ?? false;
    }
}
