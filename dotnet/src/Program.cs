using System.CommandLine;
using System.Diagnostics;
using System.Drawing;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using LibreShark.Hammerhead.CheatDbs;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.N64;
using LibreShark.Hammerhead.Roms;

namespace LibreShark.Hammerhead;

/*


*/

/*

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

*/

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cli = new Cli();
        cli.Always += (_, @params) => PrintBanner(@params);
        cli.OnInfo += (_, @params) => PrintFileInfo(@params);
        return await cli.RootCommand.InvokeAsync(args);
    }

    private static void PrintBanner(CmdParams cmdParams)
    {
        if (cmdParams.HideBanner)
        {
            return;
        }
        // ANSI color ASCII art generated with
        // https://github.com/TheZoraiz/ascii-image-converter
        Console.WriteLine();
        Console.WriteLine(Resources.GAMESHARK_LOGO_ASCII_ART_ANSI_TXT.Trim());
        Console.WriteLine(Resources.LIBRESHARK_WORDMARK_ASCII_ART_PLAIN_TXT);
    }

    private static void PrintFileInfo(InfoCmdParams @params)
    {
        foreach (FileInfo romFile in @params.InputFiles)
        {
            var rom = Rom.FromFile(romFile.FullName);
            var cheatDb = CheatDb.FromFile(romFile.FullName);
            TerminalPrinter? printer = null;
            if (rom.IsValidFormat())
            {
                printer = new TerminalPrinter(rom, @params.PrintFormat);
            }
            else if (cheatDb.IsValidFormat())
            {
                printer = new TerminalPrinter(cheatDb, @params.PrintFormat);
            }
            printer?.PrintDetails(romFile, @params);
        }
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
