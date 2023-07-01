using System.Globalization;
using System.Text.RegularExpressions;
using Google.Protobuf.Collections;
using LibreShark.Hammerhead.Codecs;
using NeoSmart.PrettySize;
using Spectre.Console;

namespace LibreShark.Hammerhead.IO;

public class TerminalPrinter
{
    #region Fields

    public bool IsColor => _printFormat == PrintFormatId.Color;
    public bool IsMarkdown => _printFormat == PrintFormatId.Markdown;
    public bool IsPlain => !IsColor && !IsMarkdown;

    private readonly AbstractCodec _codec;
    private readonly PrintFormatId _printFormat;

    #endregion

    public TerminalPrinter(AbstractCodec? codec = null, PrintFormatId printFormat = PrintFormatId.Detect)
    {
        _codec = codec ?? UnknownCodec.Create("", Array.Empty<byte>());
        _printFormat = GetEffectivePrintFormatId(printFormat);
    }

    #region Tables

    public Table BuildTable(TableBorder? colorBorderStyle = null)
    {
        var table = new Table();

        if (IsMarkdown)
        {
            table.Border = TableBorder.Markdown;
        }
        else if (IsPlain)
        {
            table.Border = TableBorder.Simple;
            table.UseSafeBorder = true;
        }
        else if (colorBorderStyle != null)
        {
            table.Border = colorBorderStyle;
        }

        return table;
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
        if (IsColor)
        {
            AnsiConsole.Write($"\n[i dim]{message}[/]\n");
            return;
        }
        Console.Error.WriteLine($"\n{message}\n");
    }

    public void PrintError(Exception e)
    {
        string message = e.ToString();
        if (!message.ToUpperInvariant().Contains("ERROR"))
        {
            message = $"ERROR: {message}";
        }
        if (IsColor)
        {
            AnsiConsole.WriteException(e);
            // AnsiConsole.Write($"\n[b red]{message}[/]\n");
            return;
        }
        Console.Error.WriteLine($"\n{message}\n");
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
        Console.WriteLine();
        Console.WriteLine(
            IsColor
                ? Resources.GAMESHARK_LOGO_ASCII_ART_ANSI_TXT.TrimEnd()
                : Resources.GAMESHARK_LOGO_ASCII_ART_PLAIN_TXT);

        string libreShark = AnsiConsole.Profile.Width >= 100 ? "LibreShark" : "Libre Shark";
        FigletFont figletFont = FigletFont.Load(new MemoryStream(Resources.FIGLET_FONT_BIG_MONEY_NW));
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
            var panel = new Panel(Bold(title))
            {
                Border = IsColor ? BoxBorder.Double : BoxBorder.Ascii,
                BorderStyle = IsColor ? new Style(foreground: ConsoleColor.DarkGray, decoration: Decoration.Bold) : Style.Plain,
                UseSafeBorder = IsPlain,
                Width = 80,
            };
            AnsiConsole.Write(panel);
        }
        Console.WriteLine();
    }

    public void PrintFileInfo(FileInfo inputFile, InfoCmdParams infoParams)
    {
        string consoleName = _codec.Metadata.ConsoleId.ToAbbreviation();
        string brandName = _codec.Metadata.BrandId.ToDisplayString();
        string headingText = $"{consoleName} {brandName}";

        PrintFilePath(inputFile);

        if (IsMarkdown)
        {
            Console.WriteLine($"**{headingText}**");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine();
            FigletFont figletFont = FigletFont.Load(new MemoryStream(
                IsColor ? Resources.FIGLET_FONT_ANSI_SHADOW : Resources.FIGLET_FONT_STANDARD
            ));
            FigletText brandAsciiArt = new FigletText(figletFont, headingText).LeftJustified();
            AnsiConsole.Write(brandAsciiArt);
        }

        PrintFilePropTable(infoParams);
        PrintChecksums(infoParams);
        PrintIdentifiers(infoParams);
        PrintFileNameRefs(infoParams);
        _codec.PrintCustomHeader(this, infoParams);
        _codec.PrintGames(this, infoParams);
        _codec.PrintCustomBody(this, infoParams);
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

        var ruleStart = new Rule(Green(shortName))
        {
            Justification = Justify.Left,
            Style = new Style(foreground: ConsoleColor.Green, decoration: Decoration.Bold)
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

        Table table = BuildTable()
                .AddColumn(HeaderCell("Property"))
                .AddColumn(HeaderCell("Value"))
            ;

        string fileSize = $"{PrettySize.Format(_codec.Buffer.Length)} " +
                          $"(0x{_codec.Buffer.Length:X8} = {_codec.Buffer.Length} bytes)";

        string buildDateDisplay = !string.IsNullOrWhiteSpace(_codec.Metadata.BuildDateIso)
            ? _codec.Metadata.BuildDateIso
            : _codec.Metadata.BuildDateRaw?.Value ?? "";
        if (buildDateDisplay.Length == 2)
        {
            buildDateDisplay = "19" + buildDateDisplay;
        }

        bool isKnownVersion = _codec.Metadata.IsKnownVersion;
        string isKnownVersionStr = isKnownVersion.ToString();
        if (IsColor)
        {
            isKnownVersionStr = isKnownVersion ? Green(isKnownVersionStr) : Red(isKnownVersionStr);
        }

        table.AddRow("File format", OrUnknown(_codec.Metadata.CodecId.ToDisplayString().EscapeMarkup()));
        table.AddRow("Platform", OrUnknown(_codec.Metadata.ConsoleId.ToDisplayString().EscapeMarkup()));
        table.AddRow("Brand", OrUnknown(GetDisplayBrand().EscapeMarkup()));
        table.AddRow("Locale", OrUnknown(GetDisplayLocale().EscapeMarkup()));
        table.AddRow("", "");
        table.AddRow("Version (internal)", OrUnknown(_codec.Metadata.DisplayVersion.EscapeMarkup()));
        table.AddRow("Build date", OrUnknown(buildDateDisplay.EscapeMarkup()));
        table.AddRow("Known ROM version", isKnownVersionStr);
        table.AddRow("", "");
        table.AddRow("File size", fileSize.EscapeMarkup());

        _codec.AddFileProps(table);

        AnsiConsole.Write(table);
    }

    private void PrintChecksums(InfoCmdParams @params)
    {
        PrintHeading("File checksums");

        Table table = BuildTable()
                .AddColumn(HeaderCell("Algorithm"))
                .AddColumn(HeaderCell("Checksum"))
            ;

        ChecksumResult? checksums = _codec.Metadata.FileChecksum;
        table.AddRow("CRC-32 (standard)", OrUnknown(checksums?.Crc32Hex));
        table.AddRow("CRC-32C (Castagnoli)", OrUnknown(checksums?.Crc32CHex));
        table.AddRow("MD5", OrUnknown(checksums?.Md5Hex));
        table.AddRow("SHA-1", OrUnknown(checksums?.Sha1Hex));

        AnsiConsole.Write(table);
    }

    private void PrintIdentifiers(InfoCmdParams @params)
    {
        PrintHeading("Identifier strings");

        if (_codec.Metadata.Identifiers.Count == 0)
        {
            Console.WriteLine(Italic("No identifiers found."));
            Console.WriteLine();
            return;
        }

        Table table = BuildTable(TableBorder.Rounded);
        table
            .AddColumn(HeaderCell("Start"))
            .AddColumn(HeaderCell("End"))
            .AddColumn(new TableColumn(HeaderCell("Size")) { Alignment = Justify.Right })
            .AddColumn(new TableColumn(HeaderCell("Len")) { Alignment = Justify.Right })
            .AddColumn(HeaderCell("String"))
            ;

        foreach (RomString id in _codec.Metadata.Identifiers)
        {
            table.AddRow(
                KeyCell($"0x{id.Addr.StartIndex:X8}"),
                KeyCell($"0x{id.Addr.EndIndex:X8}"),
                KeyCell($"0x{id.Addr.Length:X}"),
                KeyCell($"{id.Addr.Length}"),
                id.Value.EscapeMarkup()
            );
        }

        AnsiConsole.Write(table);
    }

    private void PrintFileNameRefs(InfoCmdParams @params)
    {
        RepeatedField<RomString> fileNameRefs = _codec.Metadata.FileNameRefs;
        if (fileNameRefs.Count == 0)
        {
            return;
        }

        PrintHeading("File name references");

        Table table = BuildTable(TableBorder.Rounded);
        table
            .AddColumn(HeaderCell("Start"))
            .AddColumn(HeaderCell("End"))
            .AddColumn(new TableColumn(HeaderCell("Size")) { Alignment = Justify.Right })
            .AddColumn(new TableColumn(HeaderCell("Len")) { Alignment = Justify.Right })
            .AddColumn(HeaderCell("String"))
            ;

        foreach (RomString filename in fileNameRefs)
        {
            table.AddRow(
                KeyCell($"0x{filename.Addr.StartIndex:X8}"),
                KeyCell($"0x{filename.Addr.EndIndex:X8}"),
                KeyCell($"0x{filename.Addr.Length:X}"),
                KeyCell($"{filename.Addr.Length}"),
                filename.Value.EscapeMarkup()
            );
        }

        AnsiConsole.Write(table);
    }

    public void PrintGames(InfoCmdParams @params)
    {
        PrintHeading("Games and cheat codes");

        int gameCount = _codec.Games.Count;
        if (gameCount == 0)
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
        string gameCountStr = $"{gameCount}";
        string cheatCountStr = $"{allCheats.Length:N0}";
        string codeCountStr = $"{allCodes.Length:N0}";
        string gamePlural = gameCount == 1 ? "game" : "games";
        string cheatCountPlural = allCheats.Length == 1 ? "cheat" : "cheats";
        string codeCountPlural = allCodes.Length == 1 ? "code" : "codes";

        if (IsColor)
        {
            gameCountStr = Bold(gameCountStr);
            cheatCountStr = Bold(cheatCountStr);
            codeCountStr = Bold(codeCountStr);
        }

        AnsiConsole.Markup($"{gameCountStr} {gamePlural}, " +
                           $"{cheatCountStr} {cheatCountPlural}, " +
                           $"{codeCountStr} {codeCountPlural}:\n");
        Console.WriteLine();

        Table table = BuildTable()
                .AddColumn(HeaderCell("Name"))
                .AddColumn(HeaderCell("# Games/Cheats"), column => column.Alignment = Justify.Right)
                .AddColumn(HeaderCell("Warnings"))
            ;

        List<Game> sortedGames = _codec.Games.ToList();
        sortedGames.Sort((g1, g2) =>
            string.Compare(g1.GameName.Value, g2.GameName.Value, StringComparison.InvariantCulture));

        foreach (Game game in sortedGames)
        {
            string gameName = game.GameName.Value.EscapeMarkup();
            if (game.IsGameActive)
            {
                gameName = BoldUnderline(gameName);
            }
            else
            {
                gameName = Bold(gameName);
            }
            table.AddRow(gameName, Bold($"{game.Cheats.Count}"), game.Warnings.Count > 0 ? $"{game.Warnings.Count}" : "");

            if (@params.HideCheats)
            {
                continue;
            }

            foreach (Cheat cheat in game.Cheats)
            {
                int codeCount = cheat.Codes.Count;
                string cheatName = cheat.CheatName.Value.EscapeMarkup();
                table.AddRow($"  - {cheatName}", codeCount.ToString(), "");
                if (@params.HideCodes)
                {
                    continue;
                }
                foreach (Code code in cheat.Codes)
                {
                    string codeString = code.Bytes.ToCodeString(_codec.Metadata.ConsoleId);
                    table.AddRow($"    {(Dim(codeString))}", "", "");
                }
            }
        }

        AnsiConsole.Write(table);
        Console.WriteLine();
    }

    #endregion

    #region Styles

    private string InputFilePathStyle(string filePath)
    {
        if (!IsColor) return filePath;
        return $"[b i green]{filePath}[/]";
    }

    public string White(string str)
    {
        if (!IsColor) return str;
        return $"[white]{str}[/]";
    }

    public string Black(string str)
    {
        if (!IsColor) return str;
        return $"[black]{str}[/]";
    }

    public string Red(string str)
    {
        if (!IsColor) return str;
        return $"[red]{str}[/]";
    }

    public string Green(string str)
    {
        if (!IsColor) return str;
        return $"[green]{str}[/]";
    }

    public string Blue(string str)
    {
        if (!IsColor) return str;
        return $"[blue]{str}[/]";
    }

    public string DarkBlue(string str)
    {
        if (!IsColor) return str;
        return $"[darkblue]{str}[/]";
    }

    public string DarkMagenta(string str)
    {
        if (!IsColor) return str;
        return $"[darkmagenta]{str}[/]";
    }

    public string Gray(string str)
    {
        if (!IsColor) return str;
        return $"[gray]{str}[/]";
    }

    public string Dim(string str)
    {
        if (!IsColor) return str;
        return $"[dim]{str}[/]";
    }

    public string Bold(string str)
    {
        if (!IsColor) return str;
        return $"[b]{str}[/]";
    }

    public string Italic(string str)
    {
        if (!IsColor) return str;
        return $"[i]{str}[/]";
    }

    public string BoldItalic(string str)
    {
        if (!IsColor) return str;
        return $"[b i]{str}[/]";
    }

    public string Underline(string str)
    {
        if (!IsColor) return str;
        return $"[u]{str}[/]";
    }

    public string BoldUnderline(string str)
    {
        if (!IsColor) return str;
        return $"[b u]{str}[/]";
    }

    public string HeaderCell(string str)
    {
        return Bold(str);
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

    public string UnknownStyle(string str)
    {
        if (!IsColor) return str;
        return $"[i dim]{str}[/]";
    }

    public string Error(string message)
    {
        if (!IsColor) return message;
        return $"[red]{message}[/]";
    }

    public string KeyCell(string str)
    {
        return Dim(str);
    }

    public string ValueCell(string str)
    {
        return White(str);
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

    public void PrintTable(Table table)
    {
        AnsiConsole.Write(table);
    }
}
