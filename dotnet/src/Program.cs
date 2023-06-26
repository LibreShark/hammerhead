using System.Drawing;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using CommandLine;
using LibreShark.Hammerhead.N64;
using LibreShark.Hammerhead.Roms;

namespace LibreShark.Hammerhead;

internal class Options
{
    [Option('r', "read", Required = true, HelpText = "Input files to be processed.")]
    public IEnumerable<string> InputFiles { get; set; }

    // Omitting long name, defaults to name of property, ie "--verbose"
    [Option(
        Default = false,
        HelpText = "Prints all messages to standard output.")]
    public bool Verbose { get; set; }

    [Option("stdin",
        Default = false,
        HelpText = "Read from stdin")]
    public bool stdin { get; set; }

    [Value(0, MetaName = "offset", HelpText = "File offset.")]
    public long? Offset { get; set; }
}

internal static class Program
{
    private static readonly Color TableHeaderColor = Color.FromArgb(152, 114, 159);
    private static readonly Color TableKeyColor = Color.FromArgb(160, 160, 160);
    private static readonly Color TableValueColor = Color.FromArgb(230, 230, 230);

    private static int ShowUsage()
    {
        Console.WriteLine(@"
Usage: dotnet run --project dotnet/src/src.csproj -- COMMAND [...args]

Commands:

    rom-info        ROM_1.bin [ROM_2.bin ...]

                    Displays detailed information about the given ROM files.
");
        return 1;
    }

    public static int Main(string[] cliArgs)
    {
        // ANSI color ASCII art generated with
        // https://github.com/TheZoraiz/ascii-image-converter
        Console.WriteLine();
        Console.WriteLine(Resources.GAMESHARK_LOGO_ASCII_ART_ANSI_TXT.Trim());
        Console.WriteLine(Resources.LIBRESHARK_WORDMARK_ASCII_ART_PLAIN_TXT);

        if (cliArgs.Length < 1)
        {
            return ShowUsage();
        }

        CliCmd[] cmds = {
            new(id: "rom-info", minArgCount: 1, runner: PrintRomInfo),
        };

        var cmdId = cliArgs[0];
        var cmdArgs = cliArgs.Skip(1).ToArray();

        foreach (var cmd in cmds)
        {
            if (cmd.Is(cmdId))
            {
                return cmd.Run(cmdArgs);
            }
        }

        return ShowUsage();
    }

/*
.


General:
    --hide-banner

ROM info report:

    --no-report
    --report-format=auto        Default. Detect color support from terminal type and --report-file extension
    --report-format=color       Unicode tables
    --report-format=plain       ASCII tables
    --report-format=markdown    GFM tables
    --report-format=jsonproto   All parsed data in protobuf JSON format
    --report-format=textproto   All parsed data in protobuf text format
    --report-format=none

    --report-file=-                     Default. Write report to stdout
    --report-file=''                    Execute the dotNET code paths for generating a report file, but don't actually write to a file; only write to temporary RAM
    --report-file=/path/to/report.md

Input Reading:

    --input-format=auto         Default. Detect by sniffing the file contents
    --input-format=IO_FORMAT    Disable sniffing and force Hammerhead to parse all subsequent --input-files with the given format; see IO_FORMAT section for possible values

    --input-file=/path/to/cheat-cart-firmware-dump.{gb,gbc,gba,n64,z64,bin,enc,dec,whatever}    File extension is ignored; contents are sniffed instead
    --input-file=/path/to/cheat-list.{txt,whatever}                                             File extension is ignored; contents are sniffed instead
    --input-file=-                                                                              Read from stdin

    --clean    Try to reset user preferences and active game index, delete invalid cheats, etc. By default, no cleaning is performed.

Output writing:

    --output-format=none         Default. Do not write an output file.
    --output-format=auto         Detect from terminal type (e.g., if stdin is being redirected, it means we're getting piped) and --output-file extension
    --output-format=cheats       Copy the cheats from the input file to the output ROM file, but do not overwrite the ROM file's firmware or user preferences.
    --output-format=IO_FORMAT    Disable auto-detection, and force Hammerhead to output all subsequent --output-files with the given format; see IO_FORMAT section for possible values

    --encrypt     If the file format supports it (e.g., N64 GS/AR v2.5+ ROMs), encrypt the output file for compatibility with official manufacturer utilities like "N64 GS Utils"
    --scramble    If the file format supports it (e.g., Xplorer 64 ROMs), scramble/reorder the output file bytes for compatibility with chip flashers

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
