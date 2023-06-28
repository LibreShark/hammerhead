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
    --print-format=auto|color|plain|markdown|jsonproto|textproto|none

# ROMs are automatically decrypted and unscrambled every time they're read in.
hh rom encrypt    ar3.dec               [-o ar3.enc]
hh rom decrypt    ar3.enc               [-o ar3.dec]
hh rom scramble   xplorer64.dec         [-o xplorer64.enc]
hh rom unscramble xplorer64.enc         [-o xplorer64.dec]
hh rom split      n64-gs-v2.0.bin       [-o n64-gs-v2.0.part] [--clean]
hh rom combine    n64-gs-v2.0.part*.bin [-o n64-gs-v2.0.bin]

# Cheat management
hh cheats dump *.rom [--output-format=auto]
hh cheats copy [--input-format=auto] [--output-format=auto] FROM.bin [-o TO.txt]
hh cheats copy [--input-format=auto] [--output-format=auto] FROM.txt [-o TO.bin] [--clean]

.
*/

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cli = new Cli();
        cli.Always += (_, @params) =>
        {
            if (!@params.HideBanner)
            {
                PrintBanner();
            }
        };
        cli.OnInfo += (sender, @params) =>
        {
            PrintRomInfo(@params);
        };
        return await cli.RootCommand.InvokeAsync(args);
    }

    private static void PrintBanner()
    {
        // ANSI color ASCII art generated with
        // https://github.com/TheZoraiz/ascii-image-converter
        Console.WriteLine();
        Console.WriteLine(Resources.GAMESHARK_LOGO_ASCII_ART_ANSI_TXT.Trim());
        Console.WriteLine(Resources.LIBRESHARK_WORDMARK_ASCII_ART_PLAIN_TXT);
    }

    private static int PrintRomInfo(InfoCmdParams @params)
    {
        string homeDir = Environment.GetEnvironmentVariable("userdir") ?? // Windows
                         Environment.GetEnvironmentVariable("HOME") ?? // Unix/Linux
                         "~";
        foreach (FileInfo romFile in @params.InputFiles)
        {
            Console.WriteLine(
                romFile.FullName.Replace(homeDir, "~")
                    .ForegroundColor(Color.LimeGreen).SetStyle(FontStyleExt.Bold));
            Console.WriteLine();
            var rom = Rom.FromFile(romFile.FullName);
            rom.PrintSummary(@params);
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
