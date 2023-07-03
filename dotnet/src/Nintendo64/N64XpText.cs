using System.Text;
using System.Text.RegularExpressions;
using Google.Protobuf;
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

public sealed class N64XpText : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.Nintendo64;
    private const CodecId ThisCodecId = CodecId.N64Xplorer64Text;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static N64XpText Create(string filePath, u8[] rawInput)
    {
        return new N64XpText(filePath, rawInput);
    }

    public override CodecId DefaultCheatOutputCodec => ThisCodecId;

    private N64XpText(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.HasCheats = true;
        Metadata.BrandId = BrandId.Xplorer;

        Games.AddRange(ReadGames());
    }

    private List<Game> ReadGames()
    {
        var games = new List<Game>();
        var curGame = new Game();
        var curCheat = new Cheat();
        string[] lines = Buffer.SplitLines()
            .Where((line) => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        foreach (string line in lines)
        {
            if (line.StartsWith("\t\t"))
            {
                Match match = Regex.Match(line, "(?<addr>[a-fA-F0-9]{8})[ :]?(?<value>[a-fA-F0-9]{4})");
                if (!match.Success)
                {
                    Console.Error.WriteLine($"Unsupported cheat code format: '{line}'.");
                    continue;
                }
                curCheat.Codes.Add(new Code()
                {
                    CodeIndex = (u32)curCheat.Codes.Count,
                    Bytes = ByteString.CopyFrom($"{match.Groups["addr"]}{match.Groups["value"]}".HexToBytes()),
                });
            }
            else if (line.StartsWith("\t"))
            {
                curCheat = new Cheat()
                {
                    CheatIndex = (u32)curGame.Cheats.Count,
                    CheatName = line.Trim().ToRomString(),
                };
                curGame.Cheats.Add(curCheat);
            }
            else
            {
                curGame = new Game()
                {
                    GameIndex = (u32)games.Count,
                    GameName = line.Trim().ToRomString(),
                };
                games.Add(curGame);
            }
        }

        return games;
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        var sb = new StringBuilder();

        foreach (Game game in Games)
        {
            sb.AppendLine(game.GameName.Value);
            foreach (Cheat cheat in game.Cheats)
            {
                sb.AppendLine("\t" + cheat.CheatName.Value);
                foreach (Code code in cheat.Codes)
                {
                    u8[] bytes = code.Bytes.ToByteArray();
                    string codeStr = $"{bytes[..4].ToHexString()}:{bytes[4..].ToHexString()}".ToLowerInvariant();
                    sb.AppendLine($"\t\t{codeStr}");
                }
            }
        }

        Buffer = sb.ToAsciiBytes();

        return this;
    }

    public static bool Is(u8[] buffer)
    {
        string[] nonEmptyLines = GetAllNonEmptyLines(buffer)
            .Select(line => line.TrimEnd())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        bool allLinesMatch = nonEmptyLines
            .All(line =>
            {
                bool isMatch = Regex.IsMatch(line, @"^\t{0,2}[ -~]");
                return isMatch;
            });
        string? firstOrDefault = nonEmptyLines.FirstOrDefault(line =>
        {
            bool isMatch = Regex.IsMatch(line, @"^\t\t[a-fA-F0-9]{8}[ :]?[a-fA-F0-9]{4}");
            return isMatch;
        });
        bool atLeastOneCode = firstOrDefault != null;
        return allLinesMatch && atLeastOneCode;
    }

    public static bool Is(CodecId codecId)
    {
        return codecId == ThisCodecId;
    }

    private static string[] GetAllNonEmptyLines(u8[] buffer)
    {
        string[] lines = buffer.SplitLines();
        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    private static BigEndianScribe MakeScribe(byte[] rawInput)
    {
        return new BigEndianScribe(rawInput);
    }
}
