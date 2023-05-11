using System.Collections;

namespace LibreShark.Hammerhead;

using N64;

internal static class Program
{
    public static void Main(string[] args)
    {
        /*
        var srcPath = "~/dev/libreshark/sharkdumps/n64/firmware/gs-2.00-19980305-pristine.bin";
        var destPath = "~/dev/libreshark/sharkdumps/n64/firmware/gs-1.09-19980105-pristine.bin";

        var srcBytes = File.ReadAllBytes(srcPath);
        var destBytes = File.ReadAllBytes(destPath);

        for (var i = 0x0002E000; i < 0x00039000; i++)
        {
            destBytes[i] = srcBytes[i];
        }

        File.WriteAllBytes(destPath, destBytes);

        return;
        */

        // Examples.ImportGameListFromFile(
        //     "/private/var/folders/7c/x55kqht93czbz4wls69ml0v00000gn/T/gs/gs-2.10-19980825-PRISTINE.txt",
        //     "~/dev/libreshark/sharkdumps/n64/firmware/gs-2.10-19980825-ALMOST-PRISTINE.bin"
        // );
        //
        // return;

        var cheatFilePaths = new List<string>();

        foreach (var romFilePath in args)
        {
            RomInfo? romInfo = RomReader.FromBytes(File.ReadAllBytes(romFilePath));
            if (romInfo == null)
            {
                Console.Error.WriteLine("ERROR: Unable to read GS firmware file.");
                continue;
            }

            List<Game> games = romInfo.Games;
            List<Cheat> cheats = games.SelectMany((game) => game.Cheats).ToList();

            Console.WriteLine(@"--------------------------------------------");
            Console.WriteLine("");
            Console.WriteLine(Path.GetFileName(romFilePath));
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

            var cheatFileName = Path.GetFileName(Path.ChangeExtension(romFilePath, "txt"));
            var cheatFileDir = Path.Join(Path.GetTempPath(), "gs");
            var cheatFilePath = Path.Join(cheatFileDir, cheatFileName);
            Directory.CreateDirectory(cheatFileDir);
            ListWriter.ToFile(cheatFilePath, games);
            cheatFilePaths.Add(cheatFilePath);
        }

        Console.WriteLine(@"--------------------------------------------");
        Console.WriteLine("");

        foreach (var cheatFilePath in cheatFilePaths)
        {
            Console.WriteLine(cheatFilePath);
        }
    }
}
