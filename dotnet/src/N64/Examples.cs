// bacteriamage.wordpress.com

using System.Globalization;

namespace LibreShark.Hammerhead.N64;

public static class Examples
{
    /// <summary>
    /// Export a Controller Pak note to an MPK file from a GameShark ROM or a Games List text file.
    /// </summary>
    /// <param name="inputPath">Path to input text or ROM file</param>
    /// <param name="outputNotePath">Path to output file</param>
    /// <param name="nameOfGame">Name of the game to export</param>
    /// <returns>True if the game was found and exported; else false</returns>
    public static bool ExportNote(string inputPath, string outputNotePath, string nameOfGame)
    {
        List<Game> games;

        string fileExtension = Path.GetExtension(inputPath);

        if (Equal(fileExtension, ".txt"))
        {
            games = ListReader.ReadLines(inputPath);
        }
        else if (Equal(fileExtension, ".bin"))
        {
            games = RomReader.FromFile(inputPath)?.Games!;
        }
        else
        {
            return false;
        }

        foreach (Game game in games)
        {
            if(Equal(game.Name, nameOfGame))
            {
                NoteWriter.ToFile(game, outputNotePath);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Write the contents of Controller Pak note from an MPK file to the specified text writer.
    /// </summary>
    /// <param name="inputNotePath">Path to the note's MPK file</param>
    /// <param name="writer">Writer to write to; defaults to stdout if not specified</param>
    public static void DisplayNote(string inputNotePath, TextWriter? writer = null)
    {
        Game game = NoteReader.FromFile(inputNotePath);

        new ListWriter(writer ?? Console.Out).WriteGame(game);
    }

    /// <summary>
    /// Write the list of games from a GameShark ROM to a text file.
    /// </summary>
    /// <param name="gameSharkRomPath">Path to the ROM file to read</param>
    /// <param name="outputListFilePath">Path to the text file to write to</param>
    public static void ExportGameListFromRom(string gameSharkRomPath, string outputListFilePath)
    {
        List<Game> games = RomReader.FromFile(gameSharkRomPath)?.Games!;

        ListWriter.ToFile(outputListFilePath, games);
    }

    /// <summary>
    /// Replace the list of games in a GameShark ROM file with a new list read from a text file.
    /// </summary>
    /// <param name="inputListFilePath">Path to the input list text file</param>
    /// <param name="gameSharkRomPath">Path to the GameShark ROM file to update</param>
    public static void ImportGameListFromFile(string inputListFilePath, string gameSharkRomPath)
    {
        List<Game> games = ListReader.ReadLines(inputListFilePath);

        RomWriter.ToFile(games, gameSharkRomPath);
    }

    /// <summary>
    /// Encrypt a GameShark ROM so that it can be flashed with the official N64 utility.
    /// </summary>
    /// <param name="inputPlainTextRomPath">Path to the input ROM</param>
    /// <param name="outputEncryptedRomPath">Path to the output ROM (e.g. ar3.enc)</param>
    public static void EncryptRom(string inputPlainTextRomPath, string outputEncryptedRomPath)
    {
        RomCrypter.EncodeFile(inputPlainTextRomPath, outputEncryptedRomPath);
    }

    /// <summary>
    /// Decrypt an encoded ROM file (used by the official N64 utility) to a plain-text GameShark ROM.
    /// </summary>
    /// <param name="inputEncryptedRomPath">Path to the encrypted ROM (e.g. ar3.enc)</param>
    /// <param name="outputPlainTextRomPath">Path to plain-text output ROM file</param>
    public static void DecryptRom(string inputEncryptedRomPath, string outputPlainTextRomPath)
    {
        RomCrypter.DecodeFile(inputEncryptedRomPath, outputPlainTextRomPath);
    }

    /// <summary>
    /// Compare if two strings are equal.
    /// </summary>
    private static bool Equal(string strA, string strB)
    {
        return string.Compare(strA, strB, true, CultureInfo.InvariantCulture) == 0;
    }
}
