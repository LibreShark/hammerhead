using System.Text;
using System.Text.RegularExpressions;
using Google.Protobuf;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Nintendo64;

// ReSharper disable BuiltInTypeReferenceStyle
using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using s64 = Int64;
using u64 = UInt64;
using f64 = Double;

/// <summary>
/// Plain-text cheat file format used by Datel's official N64 PC Utils.
/// </summary>
public sealed class N64GsText : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.Nintendo64;
    private const CodecId ThisCodecId = CodecId.N64GamesharkText;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    /// <summary>
    /// The official PC utils will crash if you try to use longer names.
    /// The firmware, however, simply truncates longer names.
    /// </summary>
    private const int MaxNameLen = 30;

    public static N64GsText Create(string filePath, u8[] rawInput)
    {
        return new N64GsText(filePath, rawInput);
    }

    private enum ParserState
    {
        InList,
        InGame,
        InCheat,
    }

    public override CodecId DefaultCheatOutputCodec => ThisCodecId;

    private N64GsText(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.HasCheats = true;
        Metadata.BrandId = BrandId.Gameshark;

        Games.AddRange(ReadGames());
    }

    private List<Game> ReadGames()
    {
        var games = new List<Game>();
        var curGame = new Game();
        var curCheat = new Cheat();
        var state = ParserState.InList;
        string[] lines = Buffer.SplitLines()
            .Select((line) => line.Trim())
            .Where((line) => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        foreach (string line in lines)
        {
            Match nameMatch = Regex.Match(line, @"""(?<name>[^""]*)""\s*(?<off>\.off)?\s*(?:;\s*(?<comment>.*))?");
            Match codeMatch = Regex.Match(line, @"(?<address>[a-fA-F0-9]{8})\s(?<value>[a-fA-F0-9]{4})\s*(?:;\s*(?<comment>.*))?");
            Match endMatch = Regex.Match(line, @"\.end");
            Match emptyMatch = Regex.Match(line, @"^$|^;");

            if (emptyMatch.Success)
            {
                continue;
            }

            switch (state)
            {
                case ParserState.InList when nameMatch.Success:
                    curGame = new Game()
                    {
                        GameIndex = (u32)games.Count,
                        GameName = new RomString() { Value = nameMatch.Groups["name"].Value },
                        Comment = GetComment(nameMatch.Groups["comment"]),
                    };
                    games.Add(curGame);
                    state = ParserState.InGame;
                    break;
                case ParserState.InList:
                    Console.Error.WriteLine($"[state=InList] Unrecognized line in cheat file: '{line}'");
                    continue;
                case ParserState.InGame when endMatch.Success:
                    state = ParserState.InList;
                    continue;
                case ParserState.InGame when nameMatch.Success:
                case ParserState.InCheat when nameMatch.Success:
                    Group offMatchGroup = nameMatch.Groups["off"];
                    bool isOff = offMatchGroup is { Success: true, Value.Length: > 0 };
                    curCheat = new Cheat()
                    {
                        CheatIndex = (u32)curGame.Cheats.Count,
                        CheatName = new RomString() { Value = nameMatch.Groups["name"].Value },
                        IsCheatActive = !isOff,
                        Comment = GetComment(nameMatch.Groups["comment"]),
                    };
                    curGame.Cheats.Add(curCheat);
                    state = ParserState.InCheat;
                    break;
                case ParserState.InGame:
                    Console.Error.WriteLine($"[state=InGame] Unrecognized line in cheat file: '{line}'");
                    continue;
                case ParserState.InCheat when endMatch.Success:
                    state = ParserState.InList;
                    continue;
                case ParserState.InCheat when codeMatch.Success:
                {
                    var code = new Code()
                    {
                        CodeIndex = (u32)curCheat.Codes.Count,
                        Bytes = ByteString.CopyFrom(
                            $"{codeMatch.Groups["address"].Value}{codeMatch.Groups["value"].Value}".HexToBytes()),
                        Comment = GetComment(codeMatch.Groups["comment"]),
                    };
                    curCheat.Codes.Add(code);
                    break;
                }
                case ParserState.InCheat:
                    Console.Error.WriteLine($"[state=InCheat] Unrecognized line in cheat file: '{line}'");
                    continue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return games;
    }

    private void PrintWarning(Exception e)
    {
        var printer = new TerminalPrinter(this);
        printer.PrintWarning(e.Message);
    }

    private string TruncateName(RomString romStr)
    {
        string name = romStr.Value;
        if (name.Length > MaxNameLen)
        {
            PrintWarning(new FormatException(
                $"WARNING: Game/cheat names over {MaxNameLen} chars will CRASH the " +
                $"official Datel PC utils. " +
                $"'{name}' will be truncated to {MaxNameLen} chars."
            ));
            return name[..MaxNameLen];
        }
        return name;
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"""
;------------------------------------
;{Games.Count} Games in list
;------------------------------------

""");
        foreach (Game game in Games)
        {
            string gameNameValue = TruncateName(game.GameName);
            string gameNameComment =
                !string.IsNullOrWhiteSpace(game.Comment)
                    ? $" ; {game.Comment}"
                    : "";
            sb.AppendLine($"""
;------------------------------------
{Quote(gameNameValue)}{gameNameComment}
;------------------------------------

""");
            foreach (Cheat cheat in game.Cheats)
            {
                string cheatOff = cheat.IsCheatActive ? "" : " .off";
                string cheatNameValue = TruncateName(cheat.CheatName);
                string cheatLine = Quote(cheatNameValue) + cheatOff;
                if (!string.IsNullOrWhiteSpace(cheat.Comment))
                {
                    cheatLine += $" ; {cheat.Comment}";
                }
                sb.AppendLine(cheatLine);

                foreach (Code code in cheat.Codes)
                {
                    string codeLine = code.Bytes.ToCodeString(Metadata.ConsoleId);
                    if (!string.IsNullOrWhiteSpace(code.Comment))
                    {
                        codeLine += $" ; {code.Comment}";
                    }
                    sb.AppendLine(codeLine);
                }

                sb.AppendLine();
            }

            sb.AppendLine(".end");
            sb.AppendLine();
        }

        Buffer = sb.ToAsciiBytes();

        return this;
    }

    private static string Quote(string s)
    {
        return '"' + s + '"';
    }

    public static bool Is(u8[] buffer)
    {
        string[] lines = GetAllNonEmptyLines(buffer);
        return lines
            .Select(line => Regex.Replace(line, ";.*$", "").Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .All(s =>
            {
                bool isName = Regex.IsMatch(s, "^\"[^\"]*\"(?: \\.off)?$");
                bool isCode = Regex.IsMatch(s, "^[a-fA-F0-9]{8} [a-fA-F0-9]{4}$");
                bool isEnd = Regex.IsMatch(s, "^\\.end$");
                return isName || isCode || isEnd;
            });
    }

    public static bool Is(CodecId codecId)
    {
        return codecId == ThisCodecId;
    }

    private static string[] GetAllNonEmptyLines(u8[] buffer)
    {
        string[] lines = buffer.SplitLines();
        return lines
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    private static string GetComment(Group matchGroup)
    {
        return matchGroup.Success && !string.IsNullOrWhiteSpace(matchGroup.Value) ? matchGroup.Value : "";
    }

    private static BigEndianScribe MakeScribe(byte[] rawInput)
    {
        return new BigEndianScribe(rawInput);
    }
}
