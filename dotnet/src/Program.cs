using System.Drawing;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;

namespace LibreShark.Hammerhead;

// using Protos;

internal static class Program
{
    private static readonly Color TableHeaderColor = Color.FromArgb(152, 114, 159);
    private static readonly Color TableKeyColor = Color.FromArgb(160, 160, 160);
    private static readonly Color TableValueColor = Color.FromArgb(230, 230, 230);

    public static int ShowUsage()
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
        Console.WriteLine(Resources.N64_GS_LOGO_ASCII_ART_ANSI_TXT.Trim());
        Console.WriteLine(Resources.LIBRESHARK_WORDMARK_ASCII_ART_PLAIN_TXT.Trim());

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

    private static int PrintRomInfo(IEnumerable<string> args)
    {
        foreach (var romFilePath in args)
        {
            Rom rom = DetectRom(romFilePath);
            rom.PrintSummary();
        }
        return 0;
    }

    private static Rom DetectRom(string romFilePath)
    {
        byte[] bytes = File.ReadAllBytes(romFilePath);

        if (N64GsRom.Is(bytes))
        {
            return new N64GsRom(romFilePath, bytes);
        }
        if (N64XpRom.Is(bytes))
        {
            return new N64XpRom(romFilePath, bytes);
        }

        return new UnknownRom(romFilePath, bytes);
    }
}
