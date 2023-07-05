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
/// EverDrive-64 X7 compatible cheat code file, in plain text.
/// Made by Krikzz.
/// </summary>
public sealed class N64EdX7Text : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.Nintendo64;
    private const CodecId ThisCodecId = CodecId.N64Edx7Text;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    private const u8 CodeCommentMaxLen = 36;
    private const u8 MaxCodes = 34;

    // 80273E00!0020 NPCs Don't Attack
    private static readonly Regex CodeLineRegex =
        new Regex("^(?<addr>[a-f0-9]{8})(?<active>[! ])(?<value>[a-f0-9]{4})(?:\\s+(?<comment>.+))?$", RegexOptions.IgnoreCase);

    private static readonly Regex XofYRegex =
        new Regex(@"\s*\[[ 0-9]+(?:\s*(?:/|of)?\s*)?[ 0-9]+]\s*");

    public static N64EdX7Text Create(string filePath, u8[] rawInput)
    {
        return new N64EdX7Text(filePath, rawInput);
    }

    public override CodecId DefaultCheatOutputCodec => ThisCodecId;

    private N64EdX7Text(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.HasCheats = true;

        Metadata.ConsoleId = ConsoleId.Nintendo64;
        Metadata.BrandId = BrandId.Everdrive;

        ReadGames();
    }

    private void ReadGames()
    {
        var game = new Game()
        {
            GameIndex = 0,
            GameName = Path.GetFileNameWithoutExtension(Metadata.FilePath).ToRomString(),
        };
        Games.Add(game);

        string[] lines = GetAllNonEmptyLines(Buffer);
        foreach (string line in lines)
        {
            Match match = CodeLineRegex.Match(line);
            if (!match.Success)
            {
                throw new FormatException($"Invalid cheat code format: '{line}'.");
            }

            byte[] addr = match.Groups["addr"].Value.HexToBytes();
            byte[] value = match.Groups["value"].Value.HexToBytes();
            bool isCodeDisabled = match.Groups["active"].Value == "!";

            string[] cheatStrings = XofYRegex.Split(match.Groups["comment"].Value);
            string cheatName = cheatStrings.First();
            string comment = cheatStrings.Length > 1 ? cheatStrings[1] : "";

            Cheat curCheat;
            Cheat? prevCheat = game.Cheats.LastOrDefault(cheat => cheat.CheatName.Value == cheatName);

            if (prevCheat != null)
            {
                curCheat = prevCheat;
            }
            else
            {
                curCheat = new Cheat()
                {
                    CheatIndex = (u32)game.Cheats.Count,
                    CheatName = cheatName.ToRomString(),
                    IsCheatActive = !isCodeDisabled,
                };
                game.Cheats.Add(curCheat);
            }

            var code = new Code()
            {
                CodeIndex = (u32)curCheat.Codes.Count,
                Bytes = ByteString.CopyFrom(addr.Concat(value).ToArray()),
                Comment = comment,
                IsCodeDisabled = isCodeDisabled,
            };
            curCheat.Codes.Add(code);
        }
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        if (Games.Count > 1)
        {
            Console.Error.WriteLine(
                $"WARNING: EverDrive-64 X7 cheat files only support one game, " +
                $"but found {Games.Count}. Only the first game will be output.");
        }

        int codeLineCount = 0;
        var lines = new List<string>();

        foreach (Game game in Games)
        {
            lines.Add($"# {game.GameName.Value}");
            foreach (Cheat cheat in game.Cheats)
            {
                foreach (Code code in cheat.Codes)
                {
                    string codeString = code.Bytes.ToCodeString(Metadata.ConsoleId);
                    string codeLine = codeString;
                    if (!string.IsNullOrWhiteSpace(cheat.CheatName.Value))
                    {
                        codeLine += " " + cheat.CheatName.Value;
                    }
                    if (!string.IsNullOrWhiteSpace(code.Comment))
                    {
                        codeLine += " " + code.Comment;
                    }
                    lines.Add(codeLine);
                    codeLineCount++;
                }
            }

            // Only output the first game in the list
            break;
        }

        if (codeLineCount > MaxCodes)
        {
            Console.Error.WriteLine(
                $"WARNING: EverDrive-64 X7 does not support more than {MaxCodes} codes, " +
                $"but found {codeLineCount}. The last {codeLineCount - MaxCodes} codes " +
                "will be silently ignored by EverDrive.");
        }

        Buffer = string.Join("\n", lines).ToUtf8Bytes();

        return this;
    }

    private static bool IsPlainTextChar(u8 b)
    {
        char c = (char)b;
        return c is '\n' or '\r' or '\f' or '\t' or (>= ' ' and <= '~');
    }

    public static bool Is(u8[] buffer)
    {
        if (!buffer.All(IsPlainTextChar))
        {
            return false;
        }
        string[] lines = GetAllNonEmptyLines(buffer);
        return lines.All(line => CodeLineRegex.IsMatch(line));
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

    private static BigEndianScribe MakeScribe(byte[] rawInput)
    {
        return new BigEndianScribe(rawInput);
    }
}
