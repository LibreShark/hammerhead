using System.Drawing;
using System.Globalization;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using LibreShark.Hammerhead.Roms;
using NeoSmart.PrettySize;

namespace LibreShark.Hammerhead.IO;

public class TerminalPrinter
{
    public readonly Color TableHeaderColor = Color.FromArgb(152, 114, 159);
    public readonly Color TableKeyColor = Color.FromArgb(160, 160, 160);
    public readonly Color TableValueColor = Color.FromArgb(230, 230, 230);
    public readonly Color SelectedColor = Color.FromArgb(0, 153, 0);
    public readonly Color UnknownColor = Color.FromArgb(160, 160, 160);

    private readonly IDataProvider _file;
    private readonly PrintFormat _printFormat;

    private CellFormat HeaderCellFormat
    {
        get
        {
            if (IsColor)
            {
                return new CellFormat()
                {
                    Alignment = Alignment.Left,
                    FontStyle = FontStyleExt.Bold,
                    ForegroundColor = TableHeaderColor,
                };
            }

            return new CellFormat()
            {
                Alignment = Alignment.Left,
            };
        }
    }

    public bool IsColor => _printFormat == PrintFormat.Color;
    public bool IsMarkdown => _printFormat == PrintFormat.Markdown;
    public bool IsPlain => !IsColor && !IsMarkdown;

    private TableConfig TableConfig =>
        IsColor
            ? TableConfig.Unicode()
            : IsMarkdown
                ? TableConfig.Markdown()
                : TableConfig.Simple();

    public TerminalPrinter(IDataProvider file, PrintFormat printFormat)
    {
        _file = file;
        _printFormat = GetEffectivePrintFormat(printFormat);
    }

    public Table BuildTable(Action<TableBuilder> addColumns)
    {
        var tableBuilder = new TableBuilder(HeaderCellFormat);
        addColumns(tableBuilder);
        Table table = tableBuilder.Build();
        table.Config = TableConfig;
        return table;
    }

    public CellFormat KeyCell(
        Alignment alignment = default,
        FontStyleExt fontStyle = default,
        bool innerFormatting = true
    )
    {
        return new CellFormat(
            alignment: alignment,
            foregroundColor: IsColor ? TableKeyColor : default,
            fontStyle: IsColor ? fontStyle : default,
            innerFormatting: IsColor && innerFormatting
        );
    }

    public CellFormat ValueCell(
        Alignment alignment = default,
        FontStyleExt fontStyle = default,
        bool innerFormatting = true
    )
    {
        return new CellFormat(
            alignment: alignment,
            foregroundColor: IsColor ? TableValueColor : default,
            fontStyle: IsColor ? fontStyle : default,
            innerFormatting: IsColor && innerFormatting
        );
    }

    public CellFormat TableCell(
        Alignment alignment = default,
        Color foregroundColor = default,
        Color backgroundColor = default,
        FontStyleExt fontStyle = default,
        bool innerFormatting = default
    )
    {
        return new CellFormat(
            alignment: alignment,
            foregroundColor: IsColor ? foregroundColor : default,
            backgroundColor: IsColor ? backgroundColor : default,
            fontStyle: IsColor ? fontStyle : default,
            innerFormatting: IsColor ? innerFormatting : default
        );
    }

    public void PrintDetails(FileInfo inputFile, InfoCmdParams @params)
    {
        string homeDir = Environment.GetEnvironmentVariable("userdir") ?? // Windows
                         Environment.GetEnvironmentVariable("HOME") ?? // Unix/Linux
                         "~";

        Console.WriteLine(InputFilePathStyle(inputFile.FullName.Replace(homeDir, "~")));
        Console.WriteLine();
        PrintHeading("File properties");
        PrintFilePropTable(@params);
        PrintHeading("File checksums");
        PrintChecksums(@params);
        PrintHeading("Identifiers");
        PrintIdentifiers(@params);
        _file.PrintCustomHeader(this, @params);
        _file.PrintGames(this, @params);
        _file.PrintCustomBody(this, @params);
        Console.WriteLine();
        Console.WriteLine();
        // --------------------------------------------------------------------------------
        Console.WriteLine(Bold("".PadRight(160, '-')));
        Console.WriteLine();
        Console.WriteLine();
    }

    private string InputFilePathStyle(string filePath)
    {
        return IsColor
            ? BoldUnderline(filePath.ForegroundColor(Color.LimeGreen))
            : filePath;
    }

    public void PrintHeading(string title)
    {
        string horizontalLine = "".PadRight(80, '=');
        Console.WriteLine();
        Console.WriteLine(horizontalLine);
        Console.WriteLine($"= {title,-76} =");
        Console.WriteLine(horizontalLine);
        Console.WriteLine();
    }

    public void PrintFilePropTable(InfoCmdParams @params)
    {
        Table table = BuildTable(builder =>
        {
            builder
                .AddColumn("Property", rowsFormat: KeyCell())
                .AddColumn("Value", rowsFormat: ValueCell());
        });

        string fileSize = $"{PrettySize.Format(_file.Buffer.Length)} " +
                          $"(0x{_file.Buffer.Length:X8} = {_file.Buffer.Length} bytes)";

        table.AddRow("File format", OrUnknown(_file.Metadata.Format.ToDisplayString()));
        table.AddRow("Platform", OrUnknown(_file.Metadata.Console.ToDisplayString()));
        table.AddRow("Brand", OrUnknown(GetDisplayBrand()));
        table.AddRow("Locale", OrUnknown(GetDisplayLocale()));
        table.AddRow("", "");
        table.AddRow("Version", OrUnknown(_file.Metadata.DisplayVersion));
        table.AddRow("Build date", OrUnknown(_file.Metadata.BuildDateIso));
        table.AddRow("Known ROM version", _file.Metadata.IsKnownVersion);
        table.AddRow("", "");
        table.AddRow("File size", fileSize);

        _file.AddFileProps(table);

        Console.WriteLine(table);
    }

    public string OrUnknown(string s)
    {
        return UnknownStyle(s.OrUnknown());
    }

    public void PrintChecksums(InfoCmdParams @params)
    {
        Table filePropTable = new TableBuilder(HeaderCellFormat)
            .AddColumn("Algorithm", rowsFormat: KeyCell())
            .AddColumn("Checksum", rowsFormat: ValueCell())
            .Build();

        filePropTable.AddRow("CRC-32 (standard)", _file.Metadata.FileChecksum.Crc32Hex);
        filePropTable.AddRow("CRC-32C (Castagnoli)", _file.Metadata.FileChecksum.Crc32CHex);
        filePropTable.AddRow("MD5", _file.Metadata.FileChecksum.Md5Hex);
        filePropTable.AddRow("SHA-1", _file.Metadata.FileChecksum.Sha1Hex);

        filePropTable.Config = TableConfig;

        Console.WriteLine(filePropTable);
    }

    public void PrintIdentifiers(InfoCmdParams @params)
    {
        if (_file.Metadata.Identifiers.Count == 0)
        {
            Console.WriteLine(Italic("No identifiers found."));
            return;
        }
        foreach (RomString id in _file.Metadata.Identifiers)
        {
            Console.WriteLine($"{id.Addr.ToDisplayString()} = '{id.Value}'");
        }
    }

    public void PrintGames(InfoCmdParams @params)
    {
        PrintHeading("Games and cheat codes");

        if (_file.Games.Count == 0)
        {
            Console.WriteLine(Error("No games/cheats found."));
            return;
        }

        Game? activeGame = _file.Games.FirstOrDefault(game => game.IsGameActive);
        if (activeGame != null)
        {
            Console.WriteLine($"Active game: '{activeGame.GameName.Value}'");
            Console.WriteLine();
        }

        Cheat[] allCheats = _file.Games.SelectMany(game => game.Cheats).ToArray();
        Code[] allCodes = _file.Games.SelectMany(game => game.Cheats).SelectMany(cheat => cheat.Codes).ToArray();
        string gamePlural = _file.Games.Count == 1 ? "game" : "games";
        string cheatCountPlural = allCheats.Length == 1 ? "cheat" : "cheats";
        string codeCountPlural = allCodes.Length == 1 ? "code" : "codes";
        Console.WriteLine($"{_file.Games.Count} {gamePlural}, " +
                          $"{allCheats.Length:N0} {cheatCountPlural}, " +
                          $"{allCodes.Length:N0} {codeCountPlural}:");
        Console.WriteLine();

        Table gameTable = new TableBuilder(HeaderCellFormat)
            .AddColumn("Name", rowsFormat: KeyCell())
            .AddColumn("# Games/Cheats", rowsFormat: ValueCell(Alignment.Right))
            .AddColumn("Warnings", rowsFormat: ValueCell())
            .Build();

        gameTable.Config = TableConfig;

        List<Game> sortedGames = _file.Games.ToList();
        sortedGames.Sort((g1, g2) =>
            string.Compare(g1.GameName.Value, g2.GameName.Value, StringComparison.InvariantCulture));

        foreach (Game game in sortedGames)
        {
            string gameName = game.GameName.Value;
            if (game.IsGameActive)
            {
                gameName = BoldUnderline(gameName);
            }
            else
            {
                gameName = Bold(gameName);
            }
            gameTable.AddRow(gameName, Bold($"{game.Cheats.Count}"), game.Warnings.Count > 0 ? $"{game.Warnings.Count}" : "");

            if (@params.HideCheats)
            {
                continue;
            }

            foreach (Cheat cheat in game.Cheats)
            {
                int codeCount = cheat.Codes.Count;
                gameTable.AddRow($"  - {cheat.CheatName.Value}", codeCount, "");
                if (@params.HideCodes)
                {
                    continue;
                }
                foreach (Code code in cheat.Codes)
                {
                    gameTable.AddRow($"    {code.Bytes.ToCodeString(_file.Metadata.Console).SetStyle(FontStyleExt.None)}", "", "");
                }
            }
        }

        Console.WriteLine(gameTable);
    }

    public string GetDisplayBrand()
    {
        return _file.Metadata.Brand.ToDisplayString();
    }

    public string GetDisplayLocale()
    {
        string ietf = _file.Metadata.LanguageIetfCode;
        string locale;
        if (String.IsNullOrWhiteSpace(ietf))
        {
            locale = ietf;
        }
        else
        {
            var culture = CultureInfo.GetCultureInfo(ietf);
            locale = $"{ietf} - {culture.DisplayName}";
        }
        return locale;
    }

    #region Styles

    private string Bold(string str)
    {
        return IsColor
            ? str.SetStyle(FontStyleExt.Bold)
            : str;
    }

    private string Italic(string str)
    {
        return IsColor
            ? str.SetStyle(FontStyleExt.Italic)
            : str;
    }

    private string BoldItalic(string str)
    {
        return IsColor
            ? str.SetStyle(FontStyleExt.Bold | FontStyleExt.Italic)
            : str;
    }

    private string Underline(string str)
    {
        return IsColor
            ? str.SetStyle(FontStyleExt.Underline)
            : str;
    }

    private string BoldUnderline(string str)
    {
        return IsColor
            ? str.SetStyle(FontStyleExt.Bold | FontStyleExt.Underline)
            : str;
    }

    private string UnknownStyle(string str)
    {
        return IsColor
            ? Italic(str)
            : str;
    }

    private string Error(string message)
    {
        return IsColor
            ? message.ForegroundColor(Color.Red)
            : message;
    }

    #endregion

    #region Color support detection

    private static bool IsInputRedirected => IsConsoleSizeZero && Console.KeyAvailable;

    private static bool IsOutputRedirected => IsConsoleSizeZero && !Console.KeyAvailable;

    private static bool IsConsoleSizeZero
    {
        get
        {
            try
            {
                return 0 == Console.WindowHeight + Console.WindowWidth;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }

    public static PrintFormat GetEffectivePrintFormat(PrintFormat printFormat)
    {
        switch (printFormat)
        {
            case PrintFormat.Plain:
            case PrintFormat.Color:
            case PrintFormat.Markdown:
            case PrintFormat.Json:
            case PrintFormat.Proto:
                return printFormat;
            case PrintFormat.Detect:
            {
                // https://no-color.org/
                bool isColorDisabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
                bool isDumbTerminal = Environment.GetEnvironmentVariable("TERM") is "dumb" or "xterm";
                return isColorDisabled || isDumbTerminal || IsOutputRedirected
                    ? PrintFormat.Plain
                    : PrintFormat.Color;
            }
            case PrintFormat.UnknownPrintFormat:
            default:
                return PrintFormat.Plain;
        }
    }

    #endregion
}
