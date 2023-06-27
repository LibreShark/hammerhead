using System.Diagnostics;
using System.Drawing;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using CommandLine;
using CommandLine.Text;
using LibreShark.Hammerhead.N64;
using LibreShark.Hammerhead.Roms;

namespace LibreShark.Hammerhead;

/*

# Common flags
--overwrite    Overwrite existing output files
--hide-banner

# Display detailed information about ROM files and cheat list files.
hh info n64-*.bin --hide-banner --input-format=auto|n64_cheats_datel_memcard|... --output-format=auto|color|plain|markdown|jsonproto|textproto|none

# ROMs are automatically decrypted and unscrambled every time they're read in.
# These commands are for if you simply want to decode them without any
# additional processing.
hh write-rom --encrypt    -i n64-gs-v2.0.dec        -o n64-gs-v2.0.enc
hh write-rom --decrypt    -i n64-gs-v2.0.dec        -o n64-gs-v2.0.enc --clean
hh write-rom --scramble   -i xplorer64.dec          -o xplorer64.enc
hh write-rom --unscramble -i xplorer64.enc          -o xplorer64.dec
hh write-rom --split      -i n64-gs-v2.0.bin        -o n64-gs-v2.0.parts
hh write-rom --combine    -i n64-gs-v2.0.parts*.bin -o n64-gs-v2.0.bin

# Cheat management
hh copy-cheats --input-format=auto --output-format=auto -i FROM.bin -o TO.txt
hh copy-cheats --input-format=auto --output-format=auto -i FROM.txt -o TO.bin --clean

*/

/*
.


Input/output formats (IO_FORMAT):

Value          Read/Write support | Description
---------------------------- | -- | -----------
auto                         | RW | Default. Auto-detect the input format, or output the same format as the input if possible (otherwise the closest output format to the input)
rom                          | RW | Full binary ROM file (game enhancer firmware dump)
split                        | RW | Individual sections of the ROM file, split into separate byte arrays (e.g., header, firmware, key code list, user prefs, cheat list)
cheats_jsonproto             | RW | Plain text cheat list in generic (platform-agnostic) protobuf JSON format
cheats_textproto             | RW | Plain text cheat list in generic (platform-agnostic) protobuf text format
n64_cheats_datel_text        | RW | Plain text cheat list for Datel's official N64 GS Utils PC program
n64_cheats_datel_memcard     | RW | Binary cheat list for Datel's GameShark/AR memory card (aka Controller Pak) format
n64_cheats_ed_x7_text        | RW | Plain text cheat list for EverDrive-64 X7 flash carts
n64_cheats_xp_fcd_text       | RW | Plain text cheat list for FCD's official Xplorer 64 cheat manager PC utility
n64_cheats_pj_v1_6_text      | RW | Plain text cheat list for Project 64 v1.6 `.cht` file
n64_cheats_pj_v3_0_text      | RW | Plain text cheat list for Project 64 v3+ `.cht` file
n64_cheats_openemu_text      | RW | Plain text cheat list for OpenEMU
n64_cheats_cmgsccc_2000_text | R  | Plain text cheat list from CMGSCCC's website circa the year 2000


.
*/

internal static class Program
{
    public static int Main(string[] cliArgs)
    {
        bool printBanner = true;
        var parser = new Parser(settings =>
        {
            settings.HelpWriter = Console.Error;
        });

        InfoOptions? info = null;
        WriteRomOptions? writeRom = null;
        CopyCheatsOptions? copyCheats = null;

        ParserResult<object>? parserResult = parser
                .ParseArguments<InfoOptions, WriteRomOptions, CopyCheatsOptions>(cliArgs)
                .WithParsed<InfoOptions>(opts =>
                {
                    printBanner = !opts.HideBanner;
                    info = opts;
                })
                .WithParsed<WriteRomOptions>(opts =>
                {
                    printBanner = !opts.HideBanner;
                    writeRom = opts;
                })
                .WithParsed<CopyCheatsOptions>(opts =>
                {
                    printBanner = !opts.HideBanner;
                    copyCheats = opts;
                })
                .WithNotParsed(errors =>
                {
                    printBanner = false;
                })
            ;

        if (printBanner)
        {
            // ANSI color ASCII art generated with
            // https://github.com/TheZoraiz/ascii-image-converter
            Console.WriteLine();
            Console.WriteLine(Resources.GAMESHARK_LOGO_ASCII_ART_ANSI_TXT.Trim());
            Console.WriteLine(Resources.LIBRESHARK_WORDMARK_ASCII_ART_PLAIN_TXT);
        }

        if (info != null)
        {
            PrintRomInfo(info.InputFiles ?? new List<string>());
        }
        else if (writeRom != null)
        {
            ;
        }

        return 0;
    }

    private static int PrintRomInfo(IEnumerable<string> args)
    {
        string homeDir = Environment.GetEnvironmentVariable("userdir") ?? // Windows
                         Environment.GetEnvironmentVariable("HOME") ?? // Unix/Linux
                         "~";
        foreach (string romFilePath in args)
        {
            Console.WriteLine(romFilePath.Replace(homeDir, "~").ForegroundColor(Color.LimeGreen).SetStyle(FontStyleExt.Bold));
            Console.WriteLine();
            var rom = Rom.FromFile(romFilePath);
            rom.PrintSummary();
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
