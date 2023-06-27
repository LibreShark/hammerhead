using System.CommandLine;
using System.Diagnostics;
using System.Drawing;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using LibreShark.Hammerhead.N64;
using LibreShark.Hammerhead.Roms;

namespace LibreShark.Hammerhead;

/*


*/

/*
.


Input/output formats (IO_FORMAT):

Value                 | Description
--------------------- | -----------
auto                  | Default. Auto-detect the input format, or output the same format as the input if possible (otherwise the closest output format to the input)
rom                   | Full binary ROM file (game enhancer firmware dump)
jsonproto             | Plain text, console-agnostic protobuf JSON format
textproto             | Plain text, console-agnostic protobuf text format
n64_openemu_xml       | Plain text, console-agnostic cheat list for OpenEMU
n64_datel_text        | Plain text cheat list for Datel's official N64 GS Utils PC program
n64_datel_memcard     | Binary cheat list for Datel's GameShark/AR memory card (aka Controller Pak) format
n64_fcd_text          | Plain text cheat list for FCD's official Xplorer 64 cheat manager PC utility
n64_edx7_text         | Plain text cheat list for EverDrive-64 X7 flash carts
n64_pj64_v160_text    | Plain text cheat list for Project 64 v1.6 `.cht` file
n64_pj64_v300_text    | Plain text cheat list for Project 64 v3+ `.cht` file






# Global options
--clean
--hide-banner
--overwrite

# Display detailed information about ROM files and cheat list files.
hh info n64-*.bin \
    --input-format=auto|n64_cheats_datel_memcard|... \
    --output-format=auto|color|plain|markdown|jsonproto|textproto|none

# ROMs are automatically decrypted and unscrambled every time they're read in.
hh rom encrypt    n64-gs-v2.0.dec        [-o n64-gs-v2.0.enc]
hh rom decrypt    n64-gs-v2.0.dec        [-o n64-gs-v2.0.enc]
hh rom scramble   xplorer64.dec          [-o xplorer64.enc]
hh rom unscramble xplorer64.enc          [-o xplorer64.dec]
hh rom split      n64-gs-v2.0.bin        [-o n64-gs-v2.0.parts] [--clean]
hh rom combine    n64-gs-v2.0.parts*.bin [-o n64-gs-v2.0.bin]

# Cheat management
hh cheats dump *.rom [--output-format=auto]
hh cheats copy [--input-format=auto] [--output-format=auto] FROM.bin [-o TO.txt]
hh cheats copy [--input-format=auto] [--output-format=auto] FROM.txt [-o TO.bin] [--clean]

.
*/

internal static class Program
{
    internal delegate Option<T> OptionFactory<T>();

    private static readonly OptionFactory<bool> HideBannerOptionFactory = () => new Option<bool>(
        aliases: new string[] { "--hide-banner" },
        description: "Disable the decorative ASCII art banner."
    );
    private static readonly OptionFactory<bool> CleanOptionFactory = () => new Option<bool>(
        aliases: new string[] { "--clean" },
        description: "Attempt to remove invalid cheats, sort the game list, reset user preferences, etc."
    ) { IsHidden = true };
    private static readonly OptionFactory<bool> OverwriteOptionFactory = () => new Option<bool>(
        aliases: new string[] { "-y", "--overwrite" },
        description: "Overwrite existing output files without prompting."
    );
    private static readonly OptionFactory<IoFormat> InputFormatOptionFactory = () => new Option<IoFormat>(
        aliases: new string[] { "--input-format" },
        description: "Force Hammerhead to use a specific file format when reading input files.",
        getDefaultValue: () => IoFormat.Auto
    ) { IsHidden = true };
    private static readonly OptionFactory<IoFormat> OutputIoFormatOptionFactory = () => new Option<IoFormat>(
        aliases: new string[] { "--output-format" },
        description: "Force Hammerhead to use a specific file format when writing output files.",
        getDefaultValue: () => IoFormat.Auto
    ) { IsHidden = true };
    private static readonly OptionFactory<TerminalFormat> OutputTerminalFormatOptionFactory = () => new Option<TerminalFormat>(
        aliases: new string[] { "--output-format" },
        description: "Force Hammerhead to write a specific text format to the terminal.",
        getDefaultValue: () => TerminalFormat.Detect
    ) { IsHidden = true };
    private static readonly OptionFactory<bool> HideGamesOptionFactory = () => new Option<bool>(
        aliases: new string[] { "--hide-games" },
        description: "Do not print games to the console."
    );
    private static readonly OptionFactory<bool> HideCheatsOptionFactory = () => new Option<bool>(
        aliases: new string[] { "--hide-cheats" },
        description: "Do not print cheats to the console."
    );
    private static readonly OptionFactory<bool> HideCodesOptionFactory = () => new Option<bool>(
        aliases: new string[] { "--hide-codes" },
        description: "Do not print codes to the console."
    );

    private static bool shouldPrintBanner = true;
    private static bool shouldCleanInput = true;

    public static async Task<int> Main(string[] args)
    {
        var rootCmd = new RootCommand(
            description: string.Join(" ", new string[]
            {
                "Swiss Army Knife for reading, writing, encrypting, and decrypting firmware dumps (ROM files)",
                "and cheat code lists from 1990s-2000s video game enhancers (GameShark, Action Replay, Code Breaker,",
                "Xplorer/Xploder, etc.).",
            }));

        Option<bool> hideBannerOption = HideBannerOptionFactory();
        Option<bool> cleanOption = CleanOptionFactory();

        rootCmd.AddGlobalOption(hideBannerOption);
        rootCmd.AddGlobalOption(cleanOption);

        rootCmd.SetHandler((hideBanner, clean) =>
        {
            Console.WriteLine("rootCmd()");
            shouldPrintBanner = !hideBanner;
            shouldCleanInput = clean;
        }, hideBannerOption, cleanOption);

        Command infoCmd = MakeInfoCmd(hideBannerOption, cleanOption);
        Command romCmd = MakeRomCmd(hideBannerOption, cleanOption);
        Command cheatsCmd = MakeCheatsCmd(hideBannerOption, cleanOption);

        rootCmd.AddCommand(infoCmd);
        rootCmd.AddCommand(romCmd);
        rootCmd.AddCommand(cheatsCmd);

        // PrintRomInfo(cliArgs);

        return await rootCmd.InvokeAsync(args);
    }

    private static void MaybePrintBanner()
    {
        if (shouldPrintBanner)
        {
            // ANSI color ASCII art generated with
            // https://github.com/TheZoraiz/ascii-image-converter
            Console.WriteLine();
            Console.WriteLine(Resources.GAMESHARK_LOGO_ASCII_ART_ANSI_TXT.Trim());
            Console.WriteLine(Resources.LIBRESHARK_WORDMARK_ASCII_ART_PLAIN_TXT);
        }
    }

    private static Command MakeInfoCmd(Option<bool> hideBannerOption, Option<bool> cleanOption)
    {
        Option<IoFormat> inputFormatOption = InputFormatOptionFactory();
        Option<TerminalFormat> outputFormatOption = OutputTerminalFormatOptionFactory();
        Option<bool> hideGamesOption = HideGamesOptionFactory();
        Option<bool> hideCheatsOption = HideCheatsOptionFactory();
        Option<bool> hideCodesOption = HideCodesOptionFactory();

        Argument<FileInfo[]> filesArg = new Argument<FileInfo[]>(
            "files",
            "One or more firmware dumps (ROM files) or cheat lists to read.")
        {
            Arity = ArgumentArity.OneOrMore,
        };

        var infoCmd = new Command(
            "info",
            "Display detailed information about ROM and cheat list files.")
        {
            inputFormatOption,
            outputFormatOption,
            hideGamesOption,
            hideCheatsOption,
            hideCodesOption,
            filesArg,
        };

        infoCmd.SetHandler((IoFormat inputFormat, TerminalFormat outputFormat, bool hideGames, bool hideCheats, bool hideCodes, bool hideBanner, bool clean, FileInfo[] files) =>
            {
                shouldPrintBanner = !hideBanner;
                shouldCleanInput = clean;
                MaybePrintBanner();
                PrintRomInfo(files.Select((f) => f.FullName), !hideGames, !hideCheats, !hideCodes);
            },
            inputFormatOption,
            outputFormatOption,
            hideGamesOption,
            hideCheatsOption,
            hideCodesOption,
            hideBannerOption,
            cleanOption,
            filesArg);

        return infoCmd;
    }

    private static Command MakeRomCmd(Option<bool> hideBannerOption, Option<bool> cleanOption)
    {
        var romCmd = new Command(
            "rom",
            "Read, write, encrypt, decrypt, and edit ROM files (firmware dumps).");

        var inputFileArg = new Argument<FileInfo>(
            "input_file",
            "Path to a ROM file to read")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
        var outputFileArg = new Argument<FileInfo>(
            "output_file",
            "Path to a ROM file to write")
        {
            Arity = ArgumentArity.ZeroOrOne,
        };

        var encryptCmd = new Command(
            "encrypt",
            "Encrypt a ROM file " +
            "for compatibility with official PC update utilities and chip writers.\n" +
            "If the ROM file format does not support encryption, " +
            "the output file will be a 1:1 copy of the input.")
        {
            OverwriteOptionFactory(),
            inputFileArg,
            outputFileArg,
        };

        var decryptCmd = new Command(
            "decrypt",
            "Decrypt a ROM file.\n" +
            "If the ROM file format does not support encryption, " +
            "the output file will be a 1:1 copy of the input.")
        {
            OverwriteOptionFactory(),
            inputFileArg,
            outputFileArg,
        };

        var scrambleCmd = new Command(
            "scramble",
            "Scramble (reorder) the bytes in a ROM file " +
            "for compatibility with official PC update utilities and chip writers.\n" +
            "If the ROM file format does not support scrambling, " +
            "the output file will be a 1:1 copy of the input.")
        {
            OverwriteOptionFactory(),
            inputFileArg,
            outputFileArg,
        };

        var unscrambleCmd = new Command(
            "unscramble",
            "Unscramble (reorder) the bytes in a ROM file.\n" +
            "If the ROM file format does not support scrambling, " +
            "the output file will be a 1:1 copy of the input.")
        {
            OverwriteOptionFactory(),
            inputFileArg,
            outputFileArg,
        };

        var splitCmd = new Command(
            "split",
            "Split a ROM file into sections (e.g., header, firmware, key codes, user prefs, and cheat list) " +
            "and write each section to a separate output file.")
        {
            OverwriteOptionFactory(),
            inputFileArg,
            outputFileArg,
        };

        var combineCmd = new Command(
            "combine",
            "Combine split ROM sections into a single ROM file.")
        {
            OverwriteOptionFactory(),
            inputFileArg,
            outputFileArg,
        };

        romCmd.AddCommand(encryptCmd);
        romCmd.AddCommand(decryptCmd);
        romCmd.AddCommand(scrambleCmd);
        romCmd.AddCommand(unscrambleCmd);
        romCmd.AddCommand(splitCmd);
        romCmd.AddCommand(combineCmd);

        return romCmd;
    }

    private static Command MakeCheatsCmd(Option<bool> hideBannerOption, Option<bool> cleanOption)
    {
        var cheatsCmd = new Command(
            "cheats",
            "Read, write, copy, clean, and convert cheat code lists.");

        var dumpCommand = new Command(
            "dump",
            "Split a ROM file into sections (e.g., header, firmware, key codes, user prefs, and cheat list) " +
            "and write each section to a separate output file.")
        {
            OverwriteOptionFactory(),
        };

        var copyCommand = new Command(
            "copy",
            "Combine split ROM sections into a single ROM file.")
        {
            OverwriteOptionFactory(),
        };

        cheatsCmd.AddCommand(dumpCommand);
        cheatsCmd.AddCommand(copyCommand);

        return cheatsCmd;
    }

    private static int PrintRomInfo(IEnumerable<string> args, bool printGames, bool printCheats, bool printCodes)
    {
        string homeDir = Environment.GetEnvironmentVariable("userdir") ?? // Windows
                         Environment.GetEnvironmentVariable("HOME") ?? // Unix/Linux
                         "~";
        foreach (string romFilePath in args)
        {
            Console.WriteLine(romFilePath.Replace(homeDir, "~").ForegroundColor(Color.LimeGreen).SetStyle(FontStyleExt.Bold));
            Console.WriteLine();
            var rom = Rom.FromFile(romFilePath);
            rom.PrintSummary(printGames, printCheats, printCodes);
        }
        return 0;
    }

    private static int ScrambleXp64(IEnumerable<string> inputRomFilePaths)
    {
        foreach (var inputRomFilePath in inputRomFilePaths)
        {
            var unscrambledBytes = File.ReadAllBytes(inputRomFilePath);
            var outputRomFilePath = Path.ChangeExtension(inputRomFilePath, "scrambled.bin");

            Console.WriteLine($"Scrambling Xplorer 64 ROM file '{inputRomFilePath}' to '{outputRomFilePath}'...");

            var scrambledBytes = N64XpScrambler.ScrambleXpRom(unscrambledBytes);
            File.WriteAllBytes(outputRomFilePath, scrambledBytes);
        }

        return 0;
    }

    private static int UnscrambleXp64(IEnumerable<string> inputRomFilePaths)
    {
        foreach (var inputRomFilePath in inputRomFilePaths)
        {
            var scrambledBytes = File.ReadAllBytes(inputRomFilePath);
            var outputRomFilePath = Path.ChangeExtension(inputRomFilePath, "unscrambled.bin");

            Console.WriteLine($"Unscrambling Xplorer 64 ROM file '{inputRomFilePath}' to '{outputRomFilePath}'...");

            var unscrambledBytes = N64XpScrambler.UnscrambleXpRom(scrambledBytes);
            File.WriteAllBytes(outputRomFilePath, unscrambledBytes);
        }

        return 0;
    }
}
