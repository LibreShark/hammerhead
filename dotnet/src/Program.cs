namespace LibreShark.Hammerhead;

using N64;

internal static class Program
{
    public static void Main(string[] args)
    {
        foreach (var filePath in args)
        {
            RomInfo? romInfo = RomReader.FromBytes(File.ReadAllBytes(filePath));
            if (romInfo == null)
            {
                Console.Error.WriteLine("ERROR: Unable to read GS firmware file.");
                continue;
            }

            List<Game> games = romInfo.Games;
            List<Cheat> cheats = games.SelectMany((game) => game.Cheats).ToList();

            Console.WriteLine(@"--------------------------------------------");
            Console.WriteLine("");
            Console.WriteLine(Path.GetFileName(filePath));
            Console.WriteLine("");
            Console.WriteLine($"{romInfo.Version}");
            Console.WriteLine("");
            Console.WriteLine($"File checksums: {romInfo.Checksum}");
            Console.WriteLine("");
            if (romInfo.KeyCodes.Count > 0)
            {
                string keyCodesStr = string.Join("\n", romInfo.KeyCodes.Select((kc) => kc.ToString()));
                Console.WriteLine($"{keyCodesStr}");
            }
            else
            {
                Console.WriteLine("No key codes found.");
            }
            Console.WriteLine("");
            Console.WriteLine($"{cheats.Count:N0} cheats for {games.Count:N0} games");
            Console.WriteLine("");
            // ListWriter.ToStdOut(games);
        }
    }
}
