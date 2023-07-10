using Google.Protobuf;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Codecs;
using Spectre.Console;

namespace LibreShark.Hammerhead.Cli;

public interface ICliPrinter
{
    bool IsColor { get; }
    bool IsMarkdown { get; }
    bool IsPlain { get; }
    Table BuildTable(TableBorder? colorBorderStyle = null);
    void PrintLine(string message);
    void PrintHint(string message);
    void PrintWarning(string message);
    void PrintError(string message);
    void PrintError(Exception e);
    void PrintJson(JsonFormatter formatter, IMessage proto);
    void PrintBanner(CmdParams cmdParams);
    void PrintHeading(string title);
    void PrintFileInfo(string inputFilePath, InfoCmdParams infoParams);
    void PrintFileInfo(FileInfo inputFile, InfoCmdParams infoParams);
    void PrintGames(InfoCmdParams @params);
    string White(string str);
    string Black(string str);
    string Red(string str);
    string Green(string str);
    string Blue(string str);
    string DarkBlue(string str);
    string DarkMagenta(string str);
    string Gray(string str);
    string Dim(string str);
    string Bold(string str);
    string Italic(string str);
    string BoldItalic(string str);
    string Underline(string str);
    string BoldUnderline(string str);
    string Strikethrough(string str);
    string Invert(string str);
    string HeaderCell(string str);
    string OrUnknown(string? s);
    string UnknownStyle(string str);
    string Error(string message);
    string KeyCell(string str);
    string ValueCell(string str);
    void PrintRomCommand(string heading, FileInfo inputFile, FileInfo outputFile, Action action);

    void PrintCheatsCommand(
        string heading,
        FileInfo inputFile, ICodec inputCodec,
        FileInfo outputFile, ICodec outputCodec,
        Action action);

    void PrintTable(Table table);
    void PrintN64ActiveKeyCode(Code kc);
    string FormatN64KeyCodeName(Code kc);
    string FormatN64KeyCodeBytes(Code kc);
}
