using System.Collections.Immutable;
using System.Drawing;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;

namespace LibreShark.Hammerhead;

using N64;

internal delegate int Runner(string[] args);

internal class Command
{
    private readonly string _id;
    private readonly int _minArgCount;
    private readonly int _maxArgCount;
    private readonly Runner _runner;

    public Command(string id, int minArgCount = 0, int maxArgCount = int.MaxValue, Runner? runner = null)
    {
        _id = id;
        _minArgCount = minArgCount;
        _maxArgCount = maxArgCount;
        _runner = runner ?? (args => 0);
    }

    public bool IsMatch(string id)
    {
        return string.Equals(_id, id);
    }

    public int Run(IEnumerable<string> args)
    {
        var arr = args.ToArray();
        var isOutOfBounds = arr.Length < _minArgCount || arr.Length > _maxArgCount - 1;
        return isOutOfBounds ? ShowUsage() : _runner(arr);
    }

    public static int ShowUsage()
    {
        Console.WriteLine(@"
Usage: dotnet run --project dotnet/src/src.csproj -- COMMAND [...args]

Commands:

    rom-info        GS_ROM_1.bin [GS_ROM_2.bin ...]

                    Displays detailed information about the given GS ROM files.

    export-cheats   GS_ROM_1.bin [GS_ROM_2.bin ...]

                    Dumps the cheat lists from the given GS ROMs to
                    Datel-formatted plain text files in the temp directory.

    import-cheats   FROM_DATEL_FORMATTED.txt TO_GS_ROM_1.bin [TO_GS_ROM_2.bin ...]

                    Writes a Datel-formatted plain text cheat list to the given
                    GS ROM files.

    copy-cheats     FROM_GS_ROM.bin TO_GS_ROM_1.bin [TO_GS_ROM_2.bin ...]

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
        return 1;
    }
}

internal static class Program
{
    private static readonly Color TableHeaderColor = Color.FromArgb(152, 114, 159);
    private static readonly Color TableKeyColor = Color.FromArgb(160, 160, 160);
    private static readonly Color TableValueColor = Color.FromArgb(230, 230, 230);

    public static int Main(string[] cliArgs)
    {
        if (cliArgs.Length < 1)
        {
            return Command.ShowUsage();
        }

        Command[] cmds = {
            new(id: "rom-info", minArgCount: 1, runner: PrintRomInfo),
            new(id: "export-cheats", minArgCount: 1, runner: ExportCheats),
            new(id: "import-cheats", minArgCount: 2,
                runner: (cmdArgs) => ImportCheats(cmdArgs[0], cmdArgs.Skip(1))),
            new(id: "copy-cheats", minArgCount: 2,
                runner: (cmdArgs) => CopyCheats(cmdArgs[0], cmdArgs.Skip(1))),
            new(id: "scrub-rom", minArgCount: 1, runner: ScrubRoms),
            new(id: "encrypt-rom", minArgCount: 1, runner: EncryptRoms),
            new(id: "decrypt-rom", minArgCount: 1, runner: DecryptRoms),
        };

        var cmdId = cliArgs[0];
        var cmdArgs = cliArgs.Skip(1).ToArray();

        foreach (var cmd in cmds)
        {
            if (cmd.IsMatch(cmdId))
            {
                return cmd.Run(cmdArgs);
            }
        }

        return Command.ShowUsage();
    }

    private static int CopyCheats(string srcRomFilePath, IEnumerable<string> destRomFilePaths)
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

    private static int PrintRomInfo(IEnumerable<string> romFilePaths)
    {
        foreach (var romFilePath in romFilePaths)
        {
            RomInfo? rom = RomReader.FromBytes(File.ReadAllBytes(romFilePath));
            if (rom == null)
            {
                Console.Error.WriteLine("ERROR: Unable to read GS firmware file.");
                continue;
            }

            Console.WriteLine(@"
================================================================================
");
            Console.WriteLine(Path.GetFileName(romFilePath).SetStyle(FontStyleExt.Bold).ForegroundColor(Color.LimeGreen));
            Console.WriteLine();
            Console.WriteLine("Dump integrity levels:");
            Console.WriteLine("- ⭐️ Pristine dump");
            Console.WriteLine("- ✅ Clean dump");
            Console.WriteLine("- ❌ Dirty dump");
            Console.WriteLine("- ⚠️  Unknown provenance");
            Console.WriteLine("- 🦈 LibreShark firmware (open source!)");

            Console.Write(BuildFileTable(rom));
            Console.Write(BuildKeyCodesTable(rom));
            Console.Write(BuildGameTable(rom));
        }

        return 0;
    }

    private static string BuildFileTable(RomInfo rom)
    {
        CellFormat headerFormat = new CellFormat()
        {
            Alignment = Alignment.Left,
            FontStyle = FontStyleExt.Bold,
            ForegroundColor = TableHeaderColor
        };

        Table table = new TableBuilder(headerFormat)
            .AddColumn("Property",
                rowsFormat: new CellFormat(
                    foregroundColor: TableKeyColor
                )
            )
            .AddColumn("Value")
                .RowsFormat()
                    .ForegroundColor(TableValueColor)
                    .Alignment(Alignment.Left)
                    .HasInnerFormatting()
            .Build();

        var ver = rom.Version;
        table.Config = TableConfig.Unicode();
        table
            .AddRow("Dump integrity", "Pristine | Clean | Dirty | Unknown")
            .AddRow("Brand", ver.DisplayBrand.ForegroundGradient(Color.FromArgb(180, 0, 0), Color.Red))
            .AddRow("Version", ver.DisplayNumber.ForegroundColor(Color.LimeGreen))
            .AddRow("ISO timestamp", ver.DisplayBuildTimestampIso.ForegroundColor(Color.DeepSkyBlue))
            .AddRow("Locale", ver.DisplayLocale)
            .AddRow("", "")
            .AddRow("Raw timestamp", $"'{ver.DisplayBuildTimestampRaw}'")
            .AddRow("Title version number", $"{ver.ParsedTitleVersionNumber:F2}")
            .AddRow("", "")
            .AddRow("File CRC32", rom.Checksum?.Crc32)
            .AddRow("File CRC32C", rom.Checksum?.Crc32C)
            .AddRow("File MD5", rom.Checksum?.MD5)
            .AddRow("File SHA1", rom.Checksum?.SHA1)
            .AddRow("", "")
            .AddRow("IPL3 chunk CRC32", Join(rom.ActiveKeyCode?.Ipl3ChunkCrcBytes).ForegroundColor(Color.LimeGreen))
            .AddRow("Firmware chunk CRC32", Join(rom.ActiveKeyCode?.ProgramChunkCrcBytes).ForegroundColor(Color.DeepSkyBlue))
            .AddRow("Program counter", Join(rom.ActiveKeyCode?.ProgramCounterBytes).ForegroundColor(Color.Red))
            ;
        return $"\nFile properties:\n{table}";
    }

    private static string Join(byte[]? bytes)
    {
        if (bytes == null)
        {
            return "";
        }

        return string.Join(' ', bytes.Select((b) => b.ToString("X02")));
    }

    private static string BuildKeyCodesTable(RomInfo rom)
    {
        if (rom.KeyCodes.Count == 0)
        {
            return "\nKey codes:\n" +
                   "No key codes found.".SetStyle(FontStyleExt.Bold) +
                   "\n".SetStyle(FontStyleExt.None);
        }

        CellFormat headerFormat = new CellFormat()
        {
            Alignment = Alignment.Left,
            FontStyle = FontStyleExt.Bold,
            ForegroundColor = TableHeaderColor
        };

        var hasPcBytes = rom.KeyCodes.First()?.ProgramCounterBytes.Length > 0;
        Table table = new TableBuilder(headerFormat)
            .AddColumn("Game CIC",
                rowsFormat: new CellFormat(
                    foregroundColor: TableKeyColor,
                    innerFormatting: true,
                    alignment: Alignment.Left
                )
            )
            .AddColumn("IPL3 CRC32  Firm CRC32  " + (hasPcBytes ? "ProgCounter " : "") + "Check",
                rowsFormat: new CellFormat(
                    foregroundColor: TableValueColor,
                    innerFormatting: true,
                    alignment: Alignment.Left
                )
            )
            .Build();

        var ver = rom.Version;
        table.Config = TableConfig.Unicode();

        foreach (var kc in rom.KeyCodes)
        {
            FontStyleExt activeStyle = FontStyleExt.Bold | FontStyleExt.Underline;
            FontStyleExt inactiveStyle = FontStyleExt.None;
            FontStyleExt style = kc.IsActive ? activeStyle : inactiveStyle;

            bool isActivePc =
                kc.ProgramCounterBytes.SequenceEqual(rom.ActiveKeyCode?.ProgramCounterBytes ?? new byte[] { });

            string ipl3 = FormatKeyCodeBytes(kc.Ipl3ChunkCrcBytes)
                .SetStyle(kc.IsActive ? activeStyle : inactiveStyle)
                .ForegroundColor(Color.LimeGreen);
            string fw = FormatKeyCodeBytes(kc.ProgramChunkCrcBytes)
                .SetStyle(kc.IsActive ? activeStyle : inactiveStyle)
                .ForegroundColor(Color.DeepSkyBlue);
            string pc = FormatKeyCodeBytes(kc.ProgramCounterBytes)
                .SetStyle(isActivePc ? activeStyle : inactiveStyle)
                .ForegroundColor(Color.Red);
            string check = kc.CheckDigit.ToString("X02")
                .SetStyle(kc.IsActive ? activeStyle : inactiveStyle)
                .ForegroundColor(Color.Yellow);

            // If a 32-bit Program Counter (PC) value is present in the
            // key code, add a space after it.
            //
            // All N64 ROMs (including GameShark firmware) store the PC at 0x08.
            // Firmware v2.21 and later include the PC value in their key codes;
            // v2.10 and earlier do not.
            if (kc.ProgramCounterBytes.Length > 0)
            {
                pc = " " + pc;
            }

            string nameActive   = $"> {kc.Name.SetStyle(style)}".ForegroundColor(Color.White);
            string nameInactive = $"  {kc.Name}";
            string name         = kc.IsActive ? nameActive : nameInactive;
            string keyCode      = $"{ipl3} {fw}{pc} {check}";

            table.AddRow(name, keyCode);
        }

        return $"\nKey codes: \n{table}";
    }

    private static string FormatKeyCodeBytes(byte[] bytes)
    {
        return string.Join(' ', bytes.Select((b) => b.ToString("X02")));
    }

    private static string BuildGameTable(RomInfo rom)
    {
        if (rom.Games.Count == 0)
        {
            return "\nGames (0):\n" +
                   "No games found.".SetStyle(FontStyleExt.Bold) +
                   "\n".SetStyle(FontStyleExt.None);
        }

        CellFormat headerFormat = new CellFormat()
        {
            Alignment = Alignment.Left,
            FontStyle = FontStyleExt.Bold,
            ForegroundColor = TableHeaderColor
        };

        var nameFormat = new CellFormat(
            foregroundColor: TableKeyColor,
        );
        var countFormat = new CellFormat(
            foregroundColor: TableKeyColor,
            alignment: Alignment.Right
        );
        Table table = new TableBuilder(headerFormat)
            .AddColumn("Name", rowsFormat: nameFormat)
            .AddColumn("Cheats", rowsFormat: countFormat)
            .AddColumn("Active", rowsFormat: countFormat)
            .AddColumn("Codes", rowsFormat: countFormat)
            .Build();

        var ver = rom.Version;
        table.Config = TableConfig.Unicode();

        foreach (var game in rom.Games)
        {
            table.AddRow(
                game.Name,
                game.Cheats.Count,
                game.Cheats.Count(cheat => cheat.IsActiveByDefault),
                game.Cheats.SelectMany(cheat => cheat.Codes).Count()
                );
        }

        return $"\nGames ({rom.Games.Count}):\n{table}";
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

            Console.WriteLine(@"================================================================================");
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

        Console.WriteLine(@"================================================================================");
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
