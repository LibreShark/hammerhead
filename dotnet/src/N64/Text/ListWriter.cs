// bacteriamage.wordpress.com

using System.Collections.Generic;
using System.IO;

namespace LibreShark.Hammerhead.N64
{
    /// <summary>
    /// Write a list of games to a text file in the same format used by the official utility.
    /// </summary>
    class ListWriter
    {
        private TextWriter writer;

        public static void ToFile(string path, ICollection<Game> games)
        {
            using (TextWriter writer = new StreamWriter(path))
            {
                new ListWriter(writer).WriteGames(games);
            }
        }

        public static void ToStdOut(ICollection<Game> games, TextWriter? writer = null)
        {
            new ListWriter(writer ?? Console.Out).WriteGames(games);
        }

        public ListWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        public void WriteGames(ICollection<Game> games)
        {
            WriteSeparator();
            WriteLine($";{games.Count} Games in list");
            WriteSeparator();
            WriteLine();

            foreach (Game game in games)
            {
                WriteGame(game);
            }
        }

        public void WriteGame(Game game)
        {
            WriteSeparator();
            WriteLine(Quote(game.Name));
            WriteLine();

            foreach(Cheat cheat in game.Cheats)
            {
                WriteCheat(cheat);
            }

            WriteLine(".end");
        }

        public void WriteCheat(Cheat cheat)
        {
            if (cheat.Active)
            {
                WriteLine(Quote(cheat.Name));
            }
            else
            {
                WriteLine($"{Quote(cheat.Name)} .off");
            }

            foreach (Code code in cheat.Codes)
            {
                WriteLine(code.ToString());
            }

            WriteLine();
        }

        private void WriteSeparator()
        {
            WriteLine(";------------------------------------");
        }

        private void WriteLine(string line)
        {
            writer.WriteLine(line);
        }

        private void WriteLine()
        {
            writer.WriteLine();
        }

        private static string Quote(string s)
        {
            return '"' + s + '"';
        }
    }
}
