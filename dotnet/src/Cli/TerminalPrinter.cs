using System.Globalization;
using Google.Protobuf;
using Google.Protobuf.Collections;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Codecs;
using NeoSmart.PrettySize;
using Spectre.Console;

namespace LibreShark.Hammerhead.Cli;

public class TerminalPrinter : ICliPrinter
{
    #region Fields

    public bool IsColor => _printFormat == PrintFormatId.Color;
    public bool IsMarkdown => _printFormat == PrintFormatId.Markdown;
    public bool IsPlain => !IsColor && !IsMarkdown;

    private readonly ICodec _codec;
    private readonly PrintFormatId _printFormat;

    #endregion

    public TerminalPrinter(PrintFormatId printFormat = PrintFormatId.Detect)
    {
        _codec = UnknownCodec.Create("", Array.Empty<byte>());
        _printFormat = GetEffectivePrintFormatId(printFormat);
    }

    public TerminalPrinter(ICodec codec, PrintFormatId printFormat = PrintFormatId.Detect)
    {
        _codec = codec;
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

    public void PrintLine(string message)
    {
        Console.WriteLine(message);
    }

    public void PrintHint(string message)
    {
        if (IsColor)
        {
            AnsiConsole.Write($"\n[i dim]{message}[/]\n");
            return;
        }
        Console.Error.WriteLine($"\n{message}\n");
    }

    public void PrintWarning(string message)
    {
        if (!message.ToUpperInvariant().Contains("WARNING"))
        {
            message = $"WARNING: {message}";
        }
        if (IsColor)
        {
            AnsiConsole.Markup($"\n[red]{message}[/]\n\n");
            return;
        }
        Console.Error.WriteLine($"\n{message}\n");
    }

    public void PrintError(string message)
    {
        if (!message.ToUpperInvariant().Contains("ERROR"))
        {
            message = $"ERROR: {message}";
        }
        if (IsColor)
        {
            AnsiConsole.Markup($"\n\n[red]{message.EscapeMarkup()}[/]\n\n\n");
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
            return;
        }
        Console.Error.WriteLine($"\n{message}\n");
    }

    public void PrintJson(JsonFormatter formatter, IMessage proto)
    {
        Console.WriteLine(formatter.Format(proto) + "\n");
    }

    #endregion

    #region Printing sections

    public void PrintBanner(CmdParams cmdParams)
    {
        if (cmdParams.HideBanner ||
            cmdParams.PrintFormatId is PrintFormatId.Json)
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
        FigletText asciiArt = new FigletText(figletFont, libreShark).LeftJustified();
        AnsiConsole.Write(asciiArt);
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

    public void PrintFileInfo(string inputFilePath, InfoCmdParams infoParams)
    {
        PrintFileInfo(new FileInfo(inputFilePath), infoParams);
    }

    public void PrintFileInfo(FileInfo inputFile, InfoCmdParams infoParams)
    {
        ConsoleId consoleId = _codec.Metadata.ConsoleId;
        BrandId brandId = _codec.Metadata.BrandId;
        string consoleName = consoleId.ToAbbreviation();
        string brandName = brandId.ToDisplayString();
        string headingText = $"{consoleName} {brandName}";;
        if (consoleId == ConsoleId.UnknownConsole && brandId == BrandId.UnknownBrand)
        {
            headingText = brandName;
        }

        PrintFilePath(inputFile);

        if (IsMarkdown)
        {
            Console.WriteLine($"**{headingText}**");
        }
        else if (!infoParams.HideBanner)
        {
            Console.WriteLine();
            Console.WriteLine();
            FigletFont figletFont = FigletFont.Load(new MemoryStream(
                IsColor ? Resources.FIGLET_FONT_ANSI_SHADOW : Resources.FIGLET_FONT_STANDARD
            ));
            FigletText asciiArt = new FigletText(figletFont, headingText).LeftJustified();
            AnsiConsole.Write(asciiArt);
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
            Style = new Style(foreground: ConsoleColor.Green, decoration: Decoration.Bold),
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
        if (_codec.Support.SupportsFirmware)
        {
            table.AddRow("Known firmware version", isKnownVersionStr);
        }
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
        if (_codec.Metadata.Identifiers.Count == 0)
        {
            return;
        }

        PrintHeading("Identifier strings");

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
            string str = id.Value.EscapeMarkup();
            if (id.Addr == null)
            {
                table.AddRow("", "", "", "", str);
            }
            else
            {
                table.AddRow(
                    KeyCell($"0x{id.Addr.StartIndex:X8}"),
                    KeyCell($"0x{id.Addr.EndIndex:X8}"),
                    KeyCell($"0x{id.Addr.Length:X}"),
                    KeyCell($"{id.Addr.Length}"),
                    str
                );
            }
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
            AnsiConsole.Markup(Error("No games/cheats found.") + "\n");
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
                .AddColumn(HeaderCell("# cheats/codes"), column => column.Alignment = Justify.Right)
                .AddColumn(HeaderCell("Warnings"))
            ;

        List<Game> sortedGames = _codec.Games.ToList();
        sortedGames.Sort((g1, g2) =>
            string.Compare(g1.GameName.Value, g2.GameName.Value, StringComparison.CurrentCulture));

        foreach (Game game in sortedGames)
        {
            string gameName = game.GameName.Value.EscapeMarkup();
            if (game.IsGameActive)
            {
                gameName = BoldUnderline(Green(gameName)) + " " + Italic("(active)");
            }
            else
            {
                gameName = BoldUnderline(gameName);
            }
            table.AddRow(gameName, Bold($"{game.Cheats.Count}"), game.Warnings.Count > 0 ? $"{game.Warnings.Count}" : "");
            table.AddRow("", "", "");

            if (@params.HideCheats)
            {
                continue;
            }

            foreach (Cheat cheat in game.Cheats)
            {
                int codeCount = cheat.Codes.Count;
                string cheatName = cheat.CheatName.Value.EscapeMarkup();
                if (cheat.IsCheatActive)
                {
                    cheatName = Bold(Green(cheatName)) + Italic((" (active)".EscapeMarkup()));
                }

                string cheatNameCell =
                    string.IsNullOrWhiteSpace(cheatName)
                        ? ""
                        : cheatName.StartsWith("-")
                            ? $"  {cheatName}"
                            : $"  - {cheatName}";
                table.AddRow(cheatNameCell, codeCount.ToString(), "");
                if (@params.HideCodes)
                {
                    continue;
                }
                foreach (Code code in cheat.Codes)
                {
                    string codeString = code.Bytes.ToCodeString(_codec.Metadata.ConsoleId);
                    string codeComment = string.IsNullOrWhiteSpace(code.Comment) ? "" : $"    // {code.Comment}";
                    string codeStringFormatted = code.IsCodeDisabled
                        ? Dim(Strikethrough(codeString) + "    " + BoldItalic("(disabled)"))
                        : Dim(codeString);
                    table.AddRow($"        {codeStringFormatted}{Dim(codeComment)}", "", "");
                }
            }

            if (game != sortedGames.Last())
            {
                table.AddRow("", "", "");
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
        return $"[b u green]{filePath}[/]";
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

    public string Strikethrough(string str)
    {
        if (!IsColor) return str;
        return $"[strikethrough]{str}[/]";
    }

    public string Invert(string str)
    {
        if (!IsColor) return str;
        return $"[invert]{str}[/]";
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

    public static PrintFormatId GetEffectivePrintFormatId(PrintFormatId printFormat)
    {
        switch (printFormat)
        {
            case PrintFormatId.Plain:
            case PrintFormatId.Color:
            case PrintFormatId.Markdown:
            case PrintFormatId.Json:
                return printFormat;
            case PrintFormatId.Detect:
            {
                // https://no-color.org/
                bool isColorDisabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
                bool isDumbTerminal = Environment.GetEnvironmentVariable("TERM") is "dumb" or "xterm";
                return isColorDisabled || isDumbTerminal || Console.IsOutputRedirected
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
        AnsiConsole.Markup(Bold($"{heading.EscapeMarkup()}:\n"));
        Console.WriteLine();
        AnsiConsole.Markup($"Input:  {InputFilePathStyle(inputFile.ShortName().EscapeMarkup())}\n");
        AnsiConsole.Markup($"Output: {InputFilePathStyle(outputFile.ShortName().EscapeMarkup())}\n");
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
        FileInfo inputFile, ICodec inputCodec,
        FileInfo outputFile, ICodec outputCodec,
        Action action)
    {
        Console.WriteLine();
        AnsiConsole.Markup(Bold($"{heading.EscapeMarkup()}:\n"));
        Console.WriteLine();
        string inputCodecName = inputCodec.Metadata.CodecId.ToDisplayString().EscapeMarkup();
        string outputCodecName = outputCodec.Metadata.CodecId.ToDisplayString().EscapeMarkup();
        string inputPath = InputFilePathStyle(inputFile.ShortName().EscapeMarkup());
        string outputPath = InputFilePathStyle(outputFile.ShortName().EscapeMarkup());
        AnsiConsole.Markup($"Input:  {inputPath} ({inputCodecName})\n");
        AnsiConsole.Markup($"Output: {outputPath} ({outputCodecName})\n");
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

    public string FormatN64KeyCodeName(Code kc)
    {
        string name = kc.CodeName.Value;
        return IsColor && kc.IsActiveKeyCode ? $"[green b u]{name.EscapeMarkup()}[/]" : name;
    }

    public void PrintN64ActiveKeyCode(Code kc)
    {
        string name = kc.CodeName.Value;
        string formatted = FormatN64KeyCodeBytes(kc, kc);

        if (!IsColor)
        {
            Console.WriteLine(
                $"Active key code: [ {formatted} ] - {name}");
            return;
        }

        name = $"[green b u]{name}[/]";

        AnsiConsole.Markup($"""
[dim]                   ┏━━ 64-bit checksum ━━┓ ┏ EntryPt ┓ Chk
                   ┃                     ┃ ┃         ┃  ┃[/]
Active key code: [[ {formatted} ]] - {name}

""");
    }

    public string FormatN64KeyCodeBytes(Code curKey, Code activeKey)
    {
        u8[] bytes = curKey.Bytes.ToByteArray();

        string crc1 = bytes[..4].ToHexString(" ");
        string crc2 = bytes[4..8].ToHexString(" ");
        string crc3 = bytes.Length > 9 ? bytes[8..12].ToHexString(" ") : "";
        string crc4 = new u8[] { bytes.Last() }.ToHexString();

        string sp = crc3.Length > 0 ? " " : "";

        if (IsColor && curKey.IsActiveKeyCode)
        {
            crc1 = $"[green u]{crc1}[/]";
            crc2 = $"[green u]{crc2}[/]";
            crc4 = $"[red u]{crc4}[/]";
        }

        if (IsColor && bytes.Length > 9 && activeKey.Bytes.ToByteArray().Contains(bytes[8..12]))
        {
            crc3 = $"[yellow u]{crc3}[/]";
        }

        return $"{crc1} {crc2}{sp}{crc3} {crc4}";
    }
}
