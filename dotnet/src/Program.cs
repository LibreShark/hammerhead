namespace LibreShark.Hammerhead;

using N64;

internal static class Program
{
    private static void ShowUsage()
    {
        Console.WriteLine(@"
Usage: dotnet run --project dotnet/src/src.csproj -- COMMAND [...args]

Commands:

    copy-cheats FROM_GS_ROM.bin TO_GS_ROM.bin

    import-cheats FROM_DATEL_FORMATTED.txt TO_GS_ROM.bin

    export-cheats GSROM1.bin GSROM2.z64 GSROM3.n64 ...
");
    }

    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            ShowUsage();
            return 1;
        }

        var cmd = args[0];
        if (cmd == "copy-cheats")
        {
            if (args.Length < 3)
            {
                ShowUsage();
                return 1;
            }
            return CopyGameList(args[1], args[2]);
        }
        if (cmd == "import-cheats")
        {
            if (args.Length < 3)
            {
                ShowUsage();
                return 1;
            }
            return ImportCheats(args[1], args[2]);
        }
        if (cmd == "export-cheats")
        {
            if (args.Length < 2)
            {
                ShowUsage();
                return 1;
            }
            return ExportCheats(args.Skip(1));
        }

        ShowUsage();
        return 1;
    }

    private static int CopyGameList(string fromRomFilePath, string toRomFilePath)
    {
        var srcBytes = File.ReadAllBytes(fromRomFilePath);
        var destBytes = File.ReadAllBytes(toRomFilePath);

        var srcInfo = RomReader.FromBytes(srcBytes);
        var destInfo = RomReader.FromBytes(destBytes);

        if (srcInfo == null)
        {
            Console.Error.Write($"Invalid GS ROM file source: \"{fromRomFilePath}\"");
            return 1;
        }
        if (destInfo == null)
        {
            Console.Error.Write($"Invalid GS ROM file destination: \"{toRomFilePath}\"");
            return 1;
        }

        for (var i = 0; i < 0x00010000; i++)
        {
            var to = destInfo.GameListOffset + i;
            var from = srcInfo.GameListOffset + i;
            if (to > 0x00040000 || from > 0x00040000)
            {
                break;
            }
            destBytes[to] = srcBytes[from];
        }

        File.WriteAllBytes(toRomFilePath, destBytes);

        return 0;
    }

    private static int ImportCheats(string fromDatelFormattedTextFilePath, string toGsRomFilePath)
    {
        Examples.ImportGameListFromFile(
            fromDatelFormattedTextFilePath,
            toGsRomFilePath
        );
        return 0;
    }

    private static int ExportCheats(IEnumerable<string> romFilePaths)
    {
        var cheatFilePaths = new List<string>();

        foreach (var romFilePath in romFilePaths)
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

        return 0;
    }
}
