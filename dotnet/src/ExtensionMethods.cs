using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LibreShark.Hammerhead.N64;

namespace LibreShark.Hammerhead;

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

public static class ExtensionMethods
{
    private static readonly Color UnknownColor = Color.FromArgb(160, 160, 160);

    #region Bytes

    private static readonly Encoding Ascii = Encoding.GetEncoding(
        "ascii",
        new EncoderReplacementFallback("?"),
        new DecoderReplacementFallback("?"));

    private static readonly Encoding Utf8 = Encoding.GetEncoding(
        "utf-8",
        new EncoderReplacementFallback("?"),
        new DecoderReplacementFallback("?"));

    public static string ToUtf8String(this u8[] bytes)
    {
        return Utf8.GetString(bytes);
    }

    public static string ToAsciiString(this u8[] bytes)
    {
        return Ascii.GetString(bytes);
    }

    public static string ToHexString(this IEnumerable<byte> eBytes, string delimiter = "")
    {
        return string.Join(delimiter, eBytes.Select((b) => $"{b:X2}"));
    }

    public static string ToCodeString(this IEnumerable<byte> eBytes, GameConsole console)
    {
        u8[] bytes = eBytes.ToArray();

        if (console is GameConsole.Nintendo64 or GameConsole.GameBoyAdvance)
        {
            return $"{bytes[..4].ToHexString()} {bytes[4..].ToHexString()}";
        }

        return bytes.ToHexString();
    }

    public static bool Contains(this u8[] haystackBytes, string needleStr)
    {
        return haystackBytes.Find(needleStr) > -1;
    }

    public static bool Contains(this u8[] haystackBytes, u8[] needleBytes)
    {
        return haystackBytes.Find(needleBytes) > -1;
    }

    public static s32 Find(this u8[] haystackBytes, string needleStr)
    {
        return Find(haystackBytes, Encoding.UTF8.GetBytes(needleStr));
    }

    public static s32 Find(this u8[] haystackBytes, u8[] needleBytes)
    {
        s32 needleLen = needleBytes.Length;
        s32 haystackLen = haystackBytes.Length;
        for (s32 i = 0; i < haystackLen - needleLen; i++)
        {
            s32 end = i + needleLen;
            if (haystackBytes[i..end].SequenceEqual(needleBytes))
            {
                return i;
            }
        }

        return -1;
    }

    public static s32[] FindAll(this u8[] haystack, string needleStr)
    {
        List<s32> indexes = new();
        s32 idx = 0;
        while (idx < haystack.Length)
        {
            s32 needlePos = haystack[idx..].Find(needleStr);
            if (needlePos == -1)
            {
                break;
            }
            indexes.Add(idx + needlePos);
            idx += needlePos + needleStr.Length;
        }
        return indexes.ToArray();
    }

    public static s32[] FindAll(this u8[] haystack, u8[] needleBytes)
    {
        List<s32> indexes = new();
        s32 idx = 0;
        while (idx < haystack.Length)
        {
            s32 needlePos = haystack[idx..].Find(needleBytes);
            if (needlePos == -1)
            {
                break;
            }
            indexes.Add(idx + needlePos);
            idx += needlePos + needleBytes.Length;
        }
        return indexes.ToArray();
    }

    public static bool IsKiB(this u8[] bytes, int numKiB)
    {
        return bytes.Length == numKiB * 1024;
    }

    public static bool IsMiB(this u8[] bytes, int numMiB)
    {
        return bytes.Length == numMiB * 1024 * 1024;
    }

    public static bool IsPadding(this IEnumerable<byte> eBytes)
    {
        return eBytes.All((b) => b is 0x00 or 0xFF or 0xAA);
    }

    #endregion

    #region Integers

    public static u8[] ToBigEndianBytes(this u16 n)
    {
        return new u8[]
        {
            (u8)((n >> 8) & 0xFF),
            (u8)(n & 0xFF),
        };
    }

    public static u8[] ToLittleEndianBytes(this u16 n)
    {
        return new u8[]
        {
            (u8)(n & 0xFF),
            (u8)((n >> 8) & 0xFF),
        };
    }

    public static u8[] ToBigEndianBytes(this u32 n)
    {
        return new u8[]
        {
            (u8)((n >> 24) & 0xFF),
            (u8)((n >> 16) & 0xFF),
            (u8)((n >> 8) & 0xFF),
            (u8)(n & 0xFF),
        };
    }

    public static u8[] ToLittleEndianBytes(this u32 n)
    {
        return new u8[]
        {
            (u8)(n & 0xFF),
            (u8)((n >> 8) & 0xFF),
            (u8)((n >> 16) & 0xFF),
            (u8)((n >> 24) & 0xFF),
        };
    }

    public static u8[] ToBigEndianBytes(this u64 n)
    {
        return new u8[]
        {
            (u8)((n >> 56) & 0xFF),
            (u8)((n >> 48) & 0xFF),
            (u8)((n >> 40) & 0xFF),
            (u8)((n >> 32) & 0xFF),
            (u8)((n >> 24) & 0xFF),
            (u8)((n >> 16) & 0xFF),
            (u8)((n >> 8) & 0xFF),
            (u8)(n & 0xFF),
        };
    }

    public static u8[] ToLittleEndianBytes(this u64 n)
    {
        return new u8[]
        {
            (u8)(n & 0xFF),
            (u8)((n >> 8) & 0xFF),
            (u8)((n >> 16) & 0xFF),
            (u8)((n >> 24) & 0xFF),
            (u8)((n >> 32) & 0xFF),
            (u8)((n >> 40) & 0xFF),
            (u8)((n >> 48) & 0xFF),
            (u8)((n >> 56) & 0xFF),
        };
    }

    public static u16 BigEndianToU16(this u8[] bytes)
    {
        return (u16)(bytes[0] << 8 |
                     bytes[1] << 0);
    }

    public static u16 LittleEndianToU16(this u8[] bytes)
    {
        return (u16)(bytes[0] << 0 |
                     bytes[1] << 8);
    }

    public static u32 BigEndianToU32(this u8[] bytes)
    {
        return (u32)(bytes[0]) << 24 |
               (u32)(bytes[1]) << 16 |
               (u32)(bytes[2]) << 8 |
               (u32)(bytes[3]) << 0;
    }

    public static u32 LittleEndianToU32(this u8[] bytes)
    {
        return (u32)(bytes[0]) << 0 |
               (u32)(bytes[1]) << 8 |
               (u32)(bytes[2]) << 16 |
               (u32)(bytes[3]) << 24;
    }

    public static u64 BigEndianToU64(this u8[] bytes)
    {
        return (u64)(bytes[0]) << 56 |
               (u64)(bytes[1]) << 48 |
               (u64)(bytes[2]) << 40 |
               (u64)(bytes[3]) << 32 |
               (u64)(bytes[4]) << 24 |
               (u64)(bytes[5]) << 16 |
               (u64)(bytes[6]) << 8 |
               (u64)(bytes[7]) << 0;
    }

    public static u64 LittleEndianToU64(this u8[] bytes)
    {
        return (u64)(bytes[0]) << 0 |
               (u64)(bytes[1]) << 8 |
               (u64)(bytes[2]) << 16 |
               (u64)(bytes[3]) << 24 |
               (u64)(bytes[4]) << 32 |
               (u64)(bytes[5]) << 40 |
               (u64)(bytes[6]) << 48 |
               (u64)(bytes[7]) << 56;
    }

    #endregion

    #region Protobuf

    public static RomString Trim(this RomString oldRS)
    {
        string oldValue = oldRS.Value;
        string newValue = oldValue.Trim();
        RomString newRS = new RomString
        {
            Value = newValue,
        };
        u32 startIndex = oldRS.Addr.StartIndex;
        u32 endIndex = (u32)(oldRS.Addr.EndIndex - (oldValue.Length - newValue.Length));
        u8[] oldBytes = oldRS.Addr.RawBytes.ToByteArray();
        u32 byteLen = endIndex - startIndex;
        newRS.Addr = new RomRange
        {
            StartIndex = startIndex,
            EndIndex = endIndex,
            Length = byteLen,
            RawBytes = ByteString.CopyFrom(oldBytes[..(int)byteLen]),
        };
        return newRS;
    }

    public static RomString Readable(this RomString oldRS)
    {
        var newRS = new RomString(oldRS).Trim();
        newRS.Value = String.Join(
            "",
            oldRS.Value.Select(c =>
            {
                bool isPrintable = c is >= ' ' and <= '~';
                return isPrintable ? c : ' ';
            })
        );
        return newRS;
    }

    public static string ToDisplayString(this RomRange range)
    {
        return $"[0x{range.StartIndex:X8}, 0x{range.EndIndex - 1:X8}]";
    }

    public static string ToDisplayString(this RomFormat format)
    {
        return format switch
        {
            RomFormat.GbaGamesharkDatel => "GBA - Datel GameShark",
            RomFormat.GbaGamesharkFcd => "GBA - FCD GameShark",
            RomFormat.GbaTvTuner => "GBA - TV Tuner",
            RomFormat.GbcCodebreaker => "GBC - Code Breaker",
            RomFormat.GbcGameshark => "GBC - GameShark",
            RomFormat.GbcMonsterbrain => "GBC - Monster Brain",
            RomFormat.GbcSharkMx => "GBC - Shark MX",
            RomFormat.GbcXploder => "GBC - Xploder/Xplorer",
            RomFormat.N64Gameshark => "N64 - GameShark",
            RomFormat.N64Gbhunter => "N64 - GB Hunter",
            RomFormat.N64Xplorer64 => "N64 - Xplorer 64",
            RomFormat.UnknownRomFormat => "UNKNOWN ROM format",
            _ => throw new NotSupportedException($"RomFormat {format} is missing from ToDisplayString()!"),
        };
    }

    public static string ToDisplayString(this RomBrand brand)
    {
        return brand switch
        {
            RomBrand.ActionReplay => "Action Replay",
            RomBrand.Brainboy => "BrainBoy",
            RomBrand.CodeBreaker => "Code Breaker",
            RomBrand.Equalizer => "Equalizer",
            RomBrand.GameBooster => "Game Booster",
            RomBrand.GameBuster => "Game Buster",
            RomBrand.GameGenie => "Game Genie",
            RomBrand.Gameshark => "GameShark",
            RomBrand.GbHunter => "GB Hunter",
            RomBrand.MonsterBrain => "Monster Brain",
            RomBrand.SharkMx => "Shark MX",
            RomBrand.Xploder => "Xploder",
            RomBrand.Xplorer => "Xplorer",
            RomBrand.UnknownBrand => "UNKNOWN brand",
            _ => throw new NotSupportedException($"RomBrand {brand} is missing from ToDisplayString()!"),
        };
    }

    public static string ToDisplayString(this GameConsole console)
    {
        return console switch
        {
            GameConsole.GameBoy => "Game Boy (GB)",
            GameConsole.GameBoyColor => "Game Boy Color (GBC)",
            GameConsole.GameBoyAdvance => "Game Boy Advance (GBA)",
            GameConsole.GameGear => "Game Gear (GG)",
            GameConsole.Nintendo64 => "Nintendo 64 (N64)",
            GameConsole.Playstation1 => "PlayStation 1 (PS/PS1/PSX)",
            GameConsole.Dreamcast => "Dreamcast",
            GameConsole.Gamecube => "GameCube",
            GameConsole.UnknownGameConsole => "UNKNOWN game console",
            _ => throw new NotSupportedException($"GameConsole {console} is missing from ToDisplayString()!"),
        };
    }

    public static string ToDisplayString(this N64KeyCode kc)
    {
        return $"{kc.Bytes.ToHexString(" ")}: {kc.Name.Value}" +
               (kc.IsKeyCodeActive ? " [ACTIVE]" : "");
    }

    #endregion

    #region Date/Time

    public static DateTimeOffset WithTimeZone(this DateTime dt, string tzName)
    {
        TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tzName);
        TimeSpan offset = tzInfo.BaseUtcOffset;
        string isoWithOffset = $"{dt:s}+{offset.Hours:D2}:{offset.Minutes:D2}";
        DateTimeOffset buildDateTimeWithTz = DateTimeOffset.Parse(isoWithOffset);
        return buildDateTimeWithTz;
    }

    public static DateTimeOffset WithTimeZone(this DateTimeOffset dt, string tzName)
    {
        TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tzName);
        DateTimeOffset cetTime = TimeZoneInfo.ConvertTime(dt, tzInfo);
        DateTimeOffset buildDateTimeWithTz = dt
            .Subtract(cetTime.Offset)
            .ToOffset(cetTime.Offset);
        return buildDateTimeWithTz;
    }

    public static string ToIsoString(this DateTime dt)
    {
        // Wed Nov 24 15:25:52 GMT 1999
        // 1999-11-24T15:25:52Z
        return dt.ToString("yyyy-MM-ddTHH:mm:ssK");
    }

    public static string ToIsoString(this DateTimeOffset dt)
    {
        // Wed Nov 24 15:25:52 GMT 1999
        // 1999-11-24T15:25:52Z
        return dt.ToString("yyyy-MM-ddTHH:mm:ssK");
    }

    public static string ToUtcString(this TimeSpan ts)
    {
        return "UTC" + ts.Hours switch
        {
            0 => "+00:00",
            > 0 => $"+{ts.Hours:D2}:{ts.Minutes:D2}",
            _ => $"{ts.Hours:D2}:{ts.Minutes:D2}",
        };
    }

    public static string ToUtcString(this Duration duration)
    {
        return duration.ToTimeSpan().ToUtcString();
    }

    #endregion

    #region ANSI color codes for terminals

    public static string OrUnknown(this string str, string noun = "")
    {
        noun = string.IsNullOrWhiteSpace(noun) ? "" : $" {noun}";
        return string.IsNullOrWhiteSpace(str)
            ? $"UNKNOWN{noun}".ForegroundColor(UnknownColor).SetStyle(FontStyleExt.Italic)
            : str;
    }

    #endregion

    #region Strings

    public static string[] SplitLines(this u8[] s)
    {
        return Regex.Split(s.ToAsciiString(), @"\r\f|\n");
    }

    public static string[] SplitLines(this string s)
    {
        return Regex.Split(s, @"\r\f|\n");
    }

    public static u8[] HexToBytes(this string hex)
    {
        int len = hex.Length;
        u8[] bytes = new u8[len / 2];
        for (int i = 0; i < len; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }

    #endregion

    public static T? GetValue<T>(
        this IValueDescriptor<T> symbol,
        InvocationContext context)
    {
        if (symbol is IValueSource valueSource &&
            valueSource.TryGetValue(symbol, context.BindingContext, out var boundValue) &&
            boundValue is T value)
        {
            return value;
        }
        if (symbol is Argument<T> arg)
        {
            return context.ParseResult.GetValueForArgument(arg);
        }
        if (symbol is Option<T> opt)
        {
            return context.ParseResult.GetValueForOption(opt);
        }
        throw new ArgumentException("Symbol must be an Argument<T> or Option<T>, but object is neither.");
    }
}
