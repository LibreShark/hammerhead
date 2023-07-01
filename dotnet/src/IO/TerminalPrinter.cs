using System.Globalization;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using LibreShark.Hammerhead.Codecs;
using NeoSmart.PrettySize;
using Spectre.Console;
using Color = System.Drawing.Color;
using Table = BetterConsoles.Tables.Table;

namespace LibreShark.Hammerhead.IO;

public class TerminalPrinter
{
    #region Fields

    private static readonly Color TableHeaderColor = Color.FromArgb(152, 114, 159);
    private static readonly Color TableKeyColor = Color.FromArgb(160, 160, 160);
    private static readonly Color TableValueColor = Color.FromArgb(230, 230, 230);
    private static readonly Color SelectedColor = Color.FromArgb(0, 153, 0);
    private static readonly Color UnknownColor = Color.FromArgb(160, 160, 160);
    private static readonly Color HintColor = Color.FromArgb(160, 160, 160);

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

    public bool IsColor => _printFormat == PrintFormatId.Color;
    public bool IsMarkdown => _printFormat == PrintFormatId.Markdown;
    public bool IsPlain => !IsColor && !IsMarkdown;

    private TableConfig TableConfig =>
        IsColor
            ? TableConfig.Unicode()
            : IsMarkdown
                ? TableConfig.Markdown()
                : TableConfig.Simple();

    private readonly AbstractCodec _codec;
    private readonly PrintFormatId _printFormat;

    #endregion

    public TerminalPrinter(AbstractCodec? codec = null, PrintFormatId printFormat = PrintFormatId.Detect)
    {
        _codec = codec ?? UnknownCodec.Create("", Array.Empty<byte>());
        _printFormat = GetEffectivePrintFormatId(printFormat);
    }

    #region Tables

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
        return
            IsColor
                ? new CellFormat(
                    alignment: alignment,
                    foregroundColor: TableKeyColor,
                    fontStyle: fontStyle,
                    innerFormatting: innerFormatting
                )
                : new CellFormat(alignment: alignment);
    }

    public CellFormat ValueCell(
        Alignment alignment = default,
        FontStyleExt fontStyle = default,
        bool innerFormatting = true
    )
    {
        return
            IsColor
                ? new CellFormat(
                    alignment: alignment,
                    foregroundColor: TableValueColor,
                    fontStyle: fontStyle,
                    innerFormatting: innerFormatting
                )
                : new CellFormat(alignment: alignment);
    }

    #endregion

    #region String formatting

    private string GetDisplayBrand()
    {
        return _codec.Metadata.BrandId.ToDisplayString();
    }

    private string GetDisplayLocale()
    {
        string ietf = _codec.Metadata.LanguageIetfCode;
        string locale;
        if (String.IsNullOrWhiteSpace(ietf))
        {
            locale = "";
        }
        else
        {
            var culture = CultureInfo.GetCultureInfo(ietf);
            locale = $"{ietf} - {culture.DisplayName}";
        }
        return locale;
    }

    #endregion

    #region Printing text

    public void PrintHint(string message)
    {
        string styled = IsColor
            ? Italic(message.ForegroundColor(HintColor))
            : message;
        Console.Error.WriteLine($"\n{styled}\n");
    }

    public void PrintError(string message)
    {
        if (!message.ToUpperInvariant().Contains("ERROR"))
        {
            message = $"ERROR: {message}";
        }
        string styled = IsColor
            ? Italic(message.ForegroundColor(Color.Red))
            : message;
        Console.Error.WriteLine($"\n{styled}\n");
    }

    #endregion

    #region Printing sections

    public void PrintBanner(CmdParams cmdParams)
    {
        if (cmdParams.HideBanner)
        {
            return;
        }

        // ANSI color ASCII art generated with
        // https://github.com/TheZoraiz/ascii-image-converter
        // TODO(CheatoBaggins):
        // Smallest height for N64 GameShark shark logo: 20
        // ascii-image-converter --height 20 --color --complex ~/dev/libreshark/assets/logos/n64-gameshark-logo-for-ascii-art.png
        Console.WriteLine();
        Console.WriteLine(
            IsColor
                ? Resources.GAMESHARK_LOGO_ASCII_ART_ANSI_TXT.TrimEnd()
                : Resources.GAMESHARK_LOGO_ASCII_ART_PLAIN_TXT);

        FigletFont figletFont = FigletFont.Load(new MemoryStream(Resources.FIGLET_FONT_BIG_MONEY_NW));
        string libreShark = AnsiConsole.Profile.Width >= 100 ? "LibreShark" : "Libre Shark";
        FigletText brandAsciiArt = new FigletText(figletFont, libreShark).LeftJustified();
        AnsiConsole.Write(brandAsciiArt);
    }

    public void PrintHeading(string title)
    {
        Console.WriteLine();
        if (IsMarkdown)
        {
            Console.WriteLine($"## {title}");
        }
        else
        {
            var panel = new Panel($"[b]{title}[/]")
            {
                Border = IsColor ? BoxBorder.Double : BoxBorder.Ascii,
                Width = 80,
                UseSafeBorder = IsPlain,
            };
            AnsiConsole.Write(panel);
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    public void PrintFileInfo(FileInfo inputFile, InfoCmdParams @params)
    {
        string consoleName = _codec.Metadata.ConsoleId.ToAbbreviation();
        string brandName = _codec.Metadata.BrandId.ToDisplayString();
        string headingText = $"{consoleName} {brandName}";

        if (IsColor)
        {
            FigletFont figletFont = FigletFont.Load(new MemoryStream(Resources.FIGLET_FONT_ANSI_SHADOW));
            FigletText brandAsciiArt = new FigletText(figletFont, headingText).LeftJustified();
            AnsiConsole.Write(brandAsciiArt);
        }

        PrintFilePath(inputFile);

        if (!IsColor)
        {
            Console.WriteLine($"**{headingText}**");
            Console.WriteLine();
        }

        PrintFilePropTable(@params);
        PrintChecksums(@params);
        PrintIdentifiers(@params);
        _codec.PrintCustomHeader(this, @params);
        _codec.PrintGames(this, @params);
        _codec.PrintCustomBody(this, @params);
        PrintFileDivider();
    }

    private void PrintFilePath(FileInfo inputFile)
    {
        string shortName = inputFile.ShortName();

        if (IsMarkdown || IsPlain)
        {
            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine($"# {shortName}");
            Console.WriteLine();
            return;
        }

        var ruleStart = new Rule($"[green]{shortName}[/]")
        {
            Justification = Justify.Left,
            Style = new Spectre.Console.Style(foreground: ConsoleColor.Green, decoration: Decoration.Bold)
        };
        Console.WriteLine();
        AnsiConsole.Write(ruleStart);
        Console.WriteLine();
    }

    private void PrintFileDivider()
    {
        if (IsMarkdown || IsPlain)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine();
            return;
        }

        Console.WriteLine();
        Console.WriteLine();
        AnsiConsole.Write(new Rule());
        Console.WriteLine();
        Console.WriteLine();
    }

    private void PrintFilePropTable(InfoCmdParams @params)
    {
        PrintHeading("File properties");

        Table table = BuildTable(builder =>
        {
            builder
                .AddColumn("Property", rowsFormat: KeyCell())
                .AddColumn("Value", rowsFormat: ValueCell());
        });

        string fileSize = $"{PrettySize.Format(_codec.Buffer.Length)} " +
                          $"(0x{_codec.Buffer.Length:X8} = {_codec.Buffer.Length} bytes)";

        string buildDateDisplay = !string.IsNullOrWhiteSpace(_codec.Metadata.BuildDateIso)
            ? _codec.Metadata.BuildDateIso
            : _codec.Metadata.BuildDateRaw?.Value ?? "";
        if (buildDateDisplay.Length == 2)
        {
            buildDateDisplay = "19" + buildDateDisplay;
        }

        table.AddRow("File format", OrUnknown(_codec.Metadata.CodecId.ToDisplayString()));
        table.AddRow("Platform", OrUnknown(_codec.Metadata.ConsoleId.ToDisplayString()));
        table.AddRow("Brand", OrUnknown(GetDisplayBrand()));
        table.AddRow("Locale", OrUnknown(GetDisplayLocale()));
        table.AddRow("", "");
        table.AddRow("Version (internal)", OrUnknown(_codec.Metadata.DisplayVersion));
        table.AddRow("Build date", OrUnknown(buildDateDisplay));
        table.AddRow("Known ROM version", _codec.Metadata.IsKnownVersion);
        table.AddRow("", "");
        table.AddRow("File size", fileSize);

        _codec.AddFileProps(table);

        Console.WriteLine(table);
    }

    private void PrintChecksums(InfoCmdParams @params)
    {
        PrintHeading("File checksums");

        Table filePropTable = new TableBuilder(HeaderCellFormat)
            .AddColumn("Algorithm", rowsFormat: KeyCell())
            .AddColumn("Checksum", rowsFormat: ValueCell())
            .Build();

        ChecksumResult? checksums = _codec.Metadata.FileChecksum;
        filePropTable.AddRow("CRC-32 (standard)", OrUnknown(checksums?.Crc32Hex));
        filePropTable.AddRow("CRC-32C (Castagnoli)", OrUnknown(checksums?.Crc32CHex));
        filePropTable.AddRow("MD5", OrUnknown(checksums?.Md5Hex));
        filePropTable.AddRow("SHA-1", OrUnknown(checksums?.Sha1Hex));

        filePropTable.Config = TableConfig;

        Console.WriteLine(filePropTable);
    }

    private void PrintIdentifiers(InfoCmdParams @params)
    {
        PrintHeading("Identifiers");

        if (_codec.Metadata.Identifiers.Count == 0)
        {
            Console.WriteLine(Italic("No identifiers found."));
            Console.WriteLine();
            return;
        }

        var table = new Spectre.Console.Table()
        {
            UseSafeBorder = IsPlain,
            Border = IsColor
                ? TableBorder.Rounded
                : IsMarkdown
                    ? TableBorder.Markdown
                    : TableBorder.Ascii,
        };
        table
            .AddColumn("Start")
            .AddColumn("End")
            .AddColumn(new TableColumn("Size") { Alignment = Justify.Right })
            .AddColumn(new TableColumn("Len") { Alignment = Justify.Right })
            .AddColumn("String")
            ;

        foreach (RomString id in _codec.Metadata.Identifiers)
        {
            table.AddRow(
                $"0x{id.Addr.StartIndex:X8}",
                $"0x{id.Addr.EndIndex:X8}",
                $"0x{id.Addr.Length:X}",
                $"{id.Addr.Length}",
                id.Value);
        }

        AnsiConsole.Write(table);
        Console.WriteLine();
    }

    public void PrintGames(InfoCmdParams @params)
    {
        PrintHeading("Games and cheat codes");

        if (_codec.Games.Count == 0)
        {
            Console.WriteLine(Error("No games/cheats found."));
            Console.WriteLine();
            return;
        }

        Game? activeGame = _codec.Games.FirstOrDefault(game => game.IsGameActive);
        if (activeGame != null)
        {
            Console.WriteLine($"Active game: '{activeGame.GameName.Value}'");
            Console.WriteLine();
        }

        Cheat[] allCheats = _codec.Games.SelectMany(game => game.Cheats).ToArray();
        Code[] allCodes = _codec.Games.SelectMany(game => game.Cheats).SelectMany(cheat => cheat.Codes).ToArray();
        string gamePlural = _codec.Games.Count == 1 ? "game" : "games";
        string cheatCountPlural = allCheats.Length == 1 ? "cheat" : "cheats";
        string codeCountPlural = allCodes.Length == 1 ? "code" : "codes";
        Console.WriteLine($"{_codec.Games.Count} {gamePlural}, " +
                          $"{allCheats.Length:N0} {cheatCountPlural}, " +
                          $"{allCodes.Length:N0} {codeCountPlural}:");
        Console.WriteLine();

        Table gameTable = new TableBuilder(HeaderCellFormat)
            .AddColumn("Name", rowsFormat: KeyCell())
            .AddColumn("# Games/Cheats", rowsFormat: ValueCell(Alignment.Right))
            .AddColumn("Warnings", rowsFormat: ValueCell())
            .Build();

        gameTable.Config = TableConfig;

        List<Game> sortedGames = _codec.Games.ToList();
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
                    gameTable.AddRow($"    {code.Bytes.ToCodeString(_codec.Metadata.ConsoleId).SetStyle(FontStyleExt.None)}", "", "");
                }
            }
        }

        Console.WriteLine(gameTable);
        Console.WriteLine();
    }

    #endregion

    #region Styles

    private string InputFilePathStyle(string filePath)
    {
        return IsColor
            ? BoldUnderline(filePath.ForegroundColor(Color.LimeGreen))
            : filePath;
    }

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

    public string OrUnknown(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return UnknownStyle("UNKNOWN");
        }
        if (s.ToUpperInvariant().Contains("UNKNOWN"))
        {
            return UnknownStyle(s);
        }
        return s;
    }

    private string UnknownStyle(string str)
    {
        return IsColor
            ? Italic(str.ForegroundColor(UnknownColor))
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

    public static PrintFormatId GetEffectivePrintFormatId(PrintFormatId printFormat)
    {
        switch (printFormat)
        {
            case PrintFormatId.Plain:
            case PrintFormatId.Color:
            case PrintFormatId.Markdown:
            case PrintFormatId.Json:
            case PrintFormatId.Proto:
                return printFormat;
            case PrintFormatId.Detect:
            {
                // https://no-color.org/
                bool isColorDisabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
                bool isDumbTerminal = Environment.GetEnvironmentVariable("TERM") is "dumb" or "xterm";
                return isColorDisabled || isDumbTerminal || IsOutputRedirected
                    ? PrintFormatId.Plain
                    : PrintFormatId.Color;
            }
            case PrintFormatId.UnknownPrintFormat:
            default:
                return PrintFormatId.Plain;
        }
    }

    #endregion

    public void PrintRomCommand(string heading, FileInfo inputFile, FileInfo outputFile, Action action)
    {
        Console.WriteLine();
        Console.WriteLine(Bold($"{heading}:"));
        Console.WriteLine();
        Console.WriteLine($"Input:  {InputFilePathStyle(inputFile.ShortName())}");
        Console.WriteLine($"Output: {InputFilePathStyle(outputFile.ShortName())}");
        Console.WriteLine();
        Console.WriteLine("...");
        Console.WriteLine();
        action();
        Console.WriteLine();
        Console.WriteLine("Done!");
        Console.WriteLine();
    }

    public void PrintCheatsCommand(
        string heading,
        FileInfo inputFile, AbstractCodec inputCodec,
        FileInfo outputFile, AbstractCodec outputCodec,
        Action action)
    {
        Console.WriteLine();
        Console.WriteLine(Bold($"{heading}:"));
        Console.WriteLine();
        string inputCodecName = inputCodec.Metadata.CodecId.ToDisplayString();
        string outputCodecName = outputCodec.Metadata.CodecId.ToDisplayString();
        string inputPath = InputFilePathStyle(inputFile.ShortName());
        string outputPath = InputFilePathStyle(outputFile.ShortName());
        Console.WriteLine($"Input:  {inputPath} ({inputCodecName})");
        Console.WriteLine($"Output: {outputPath} ({outputCodecName})");
        Console.WriteLine();
        Console.WriteLine("...");
        Console.WriteLine();
        action();
        Console.WriteLine();
        Console.WriteLine("Done!");
        Console.WriteLine();
    }
}
