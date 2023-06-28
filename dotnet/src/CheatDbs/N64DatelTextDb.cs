using System.Text;
using System.Text.RegularExpressions;
using Google.Protobuf;

namespace LibreShark.Hammerhead.CheatDbs;

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

public sealed class N64DatelTextDb : CheatDb
{
    private const GameConsole ThisConsole = GameConsole.Nintendo64;
    private const FileFormat ThisFileFormat = FileFormat.N64DatelText;
    private const RomFormat ThisRomFormat = RomFormat.N64Gameshark;

    public N64DatelTextDb(string filePath, u8[] rawInput)
        : base(filePath, rawInput, ThisConsole, ThisFileFormat, ThisRomFormat)
    {
        Games.AddRange(ReadGames());
    }

    private enum ParserState
    {
        InList,
        InGame,
        InCheat,
    }

    protected override List<Game> ReadGames()
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
                    };
                    games.Add(curGame);
                    state = ParserState.InGame;
                    break;
                case ParserState.InList:
                    Console.Error.WriteLine($"[state=InList] Unrecognized line in cheat file: `{line}`");
                    continue;
                case ParserState.InGame when endMatch.Success:
                    state = ParserState.InList;
                    continue;
                case ParserState.InGame when nameMatch.Success:
                case ParserState.InCheat when nameMatch.Success:
                    curCheat = new Cheat()
                    {
                        CheatIndex = (u32)curGame.Cheats.Count,
                        CheatName = new RomString() { Value = nameMatch.Groups["name"].Value },
                        IsCheatActive = !(nameMatch.Groups["off"].Success && nameMatch.Groups["off"].Value.Length > 0),
                    };
                    curGame.Cheats.Add(curCheat);
                    state = ParserState.InCheat;
                    break;
                case ParserState.InGame:
                    Console.Error.WriteLine($"[state=InGame] Unrecognized line in cheat file: `{line}`");
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
                        Comment = codeMatch.Groups["comment"].Success && !string.IsNullOrWhiteSpace(codeMatch.Groups["comment"].Value)
                            ? codeMatch.Groups["comment"].Value
                            : "",
                    };
                    curCheat.Codes.Add(code);
                    break;
                }
                case ParserState.InCheat:
                    Console.Error.WriteLine($"[state=InCheat] Unrecognized line in cheat file: `{line}`");
                    continue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return games;
    }

    protected override u8[] WriteGames(IEnumerable<Game> games)
    {
        Game[] gamesArray = games.ToArray();
        var sb = new StringBuilder();
        sb.Append($"""
;------------------------------------
;{gamesArray.Length} Games in list
;------------------------------------
""");
        foreach (Game game in gamesArray)
        {
            sb.Append($"""
;------------------------------------
{Quote(game.GameName.Value)}
;------------------------------------
""");
            foreach (Cheat cheat in game.Cheats)
            {
                string cheatOff = cheat.IsCheatActive ? "" : " .off";
                sb.AppendLine(Quote(cheat.CheatName.Value) + cheatOff);
                foreach (Code code in cheat.Codes)
                {
                    sb.Append(code.Bytes.ToCodeString(Metadata.Console));
                    if (!string.IsNullOrWhiteSpace(code.Comment))
                    {
                        sb.Append(" ; ");
                        sb.Append(code.Comment);
                    }
                    sb.AppendLine();
                }
            }

            sb.AppendLine(".end");
        }
        return sb.ToAsciiBytes();
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
}
