namespace LibreShark.Hammerhead;

using N64;

internal static class Program
{
    private static void ShowUsage()
    {
        Console.WriteLine(@"
Usage: dotnet run --project dotnet/src/src.csproj -- COMMAND [...args]

Commands:

    export-cheats   GS_ROM_1.bin [GS_ROM_2.bin ...]

                    Dumps the cheat lists from the given GS ROMs to
                    Datel-formatted plain text files in the temp directory.

    import-cheats   FROM_DATEL_FORMATTED.txt   TO_GS_ROM_1.bin [TO_GS_ROM_2.bin ...]

                    Writes a Datel-formatted plain text cheat list to the given
                    GS ROM files.

    copy-cheats     FROM_GS_ROM.bin            TO_GS_ROM_1.bin [TO_GS_ROM_2.bin ...]

                    Copies the list of cheats from one GS ROM file to another.

    scrub-rom       GS_ROM_1.bin [GS_ROM_2.bin ...]

                    Attempts to clean up garbage cheats, reset user preferences,
                    etc.

    encrypt-rom     GS_ROM_1.bin [GS_ROM_2.bin ...]

                    Encrypts the given unencrypted ROM files for use with the
                    official Datel N64 Utils.

    decrypt-rom     GS_ROM_1.bin [GS_ROM_2.bin ...]

                    Decrypts the given Datel-encrypted ROM files so that they
                    can be inspected and edited.
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
            return CopyGameList(args[1], args.Skip(2));
        }
        if (cmd == "import-cheats")
        {
            if (args.Length < 3)
            {
                ShowUsage();
                return 1;
            }
            return ImportCheats(args[1], args.Skip(2));
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
        if (cmd == "scrub-rom")
        {
            if (args.Length < 2)
            {
                ShowUsage();
                return 1;
            }
            return ScrubRoms(args.Skip(1));
        }
        if (cmd == "encrypt-rom")
        {
            if (args.Length < 2)
            {
                ShowUsage();
                return 1;
            }
            return EncryptRoms(args.Skip(1));
        }
        if (cmd == "decrypt-rom")
        {
            if (args.Length < 2)
            {
                ShowUsage();
                return 1;
            }
            return DecryptRoms(args.Skip(1));
        }

        ShowUsage();
        return 1;
    }

    private static int CopyGameList(string srcRomFilePath, IEnumerable<string> destRomFilePaths)
    {
        var srcBytes = File.ReadAllBytes(srcRomFilePath);
        var srcInfo = RomReader.FromBytes(srcBytes);
        if (srcInfo == null)
        {
            Console.Error.Write($"Invalid GS ROM file source: \"{srcRomFilePath}\"");
            return 1;
        }

        foreach (var destRomFilePath in destRomFilePaths)
        {
            var destBytes = File.ReadAllBytes(destRomFilePath);
            var destInfo = RomReader.FromBytes(destBytes);
            if (destInfo == null)
            {
                Console.Error.Write($"Invalid GS ROM file destination: \"{destRomFilePath}\"");
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

            File.WriteAllBytes(destRomFilePath, destBytes);
        }

        return 0;
    }

    private static int ImportCheats(string srcDatelFormattedTextFilePath, IEnumerable<string> destRomFilePaths)
    {
        foreach (var destRomFilePath in destRomFilePaths)
        {
            Examples.ImportGameListFromFile(
                srcDatelFormattedTextFilePath,
                destRomFilePath
            );
        }

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

    private static int ScrubRoms(IEnumerable<string> romFilePaths)
    {
        foreach (var romFilePath in romFilePaths)
        {
            RomInfo? romInfo = RomReader.FromBytes(File.ReadAllBytes(romFilePath));
            if (romInfo == null)
            {
                Console.Error.WriteLine("ERROR: Unable to read GS firmware file.");
                continue;
            }

            var outputFilePath = Path.ChangeExtension(romFilePath, "scrubbed.z64");
            Console.WriteLine($"Cleaning GS ROM file \"{romFilePath}\" -> \"{outputFilePath}\"...");
            RomWriter.ToFileAndReset(romInfo.Games, romFilePath, outputFilePath);
        }

        return 0;
    }

    private static int EncryptRoms(IEnumerable<string> romFilePaths)
    {
        foreach (var decryptedRomFilePath in romFilePaths)
        {
            var encryptedRomFilePath = Path.ChangeExtension(decryptedRomFilePath, "enc");
            Console.WriteLine($"Encrypting GS ROM file \"{decryptedRomFilePath}\" -> \"{encryptedRomFilePath}\"...");
            Examples.EncryptRom(decryptedRomFilePath, encryptedRomFilePath);
        }

        return 0;
    }

    private static int DecryptRoms(IEnumerable<string> romFilePaths)
    {
        foreach (var encryptedRomFilePath in romFilePaths)
        {
            var decryptedRomFilePath = Path.ChangeExtension(encryptedRomFilePath, "dec");
            Console.WriteLine($"Decrypting GS ROM file \"{encryptedRomFilePath}\" -> \"{decryptedRomFilePath}\"...");
            Examples.DecryptRom(encryptedRomFilePath, decryptedRomFilePath);
        }

        return 0;
    }
}
