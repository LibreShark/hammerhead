using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Force.Crc32;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

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
    /// <summary>
    /// The two primary manufacturers of video game enhancers (Datel and FCD)
    /// compiled their firmware on English Windows 9x machines and used the
    /// default codepage when encoding strings. Some strings contain bytes
    /// above 127 (e.g., the accented 'e' in "Pokèmon Bisasam") that
    /// cannot be properly decoded with ASCII, which is only 7-bit.
    /// </summary>
    private static readonly Encoding Windows1252;

    private static readonly Encoding Utf8;

    static ExtensionMethods()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Windows1252 = Encoding.GetEncoding(
            "Windows-1252",
            new EncoderReplacementFallback("?"),
            new DecoderReplacementFallback("?")
        );
        Utf8 = Encoding.GetEncoding(
            "utf-8",
            new EncoderReplacementFallback("?"),
            new DecoderReplacementFallback("?")
        );
    }

    #region Bytes

    public static string ToUtf8String(this u8[] bytes)
    {
        return Utf8.GetString(bytes);
    }

    public static u8[] ToUtf8Bytes(this string str)
    {
        return Utf8.GetBytes(str);
    }

    public static u8[] ToUtf8Bytes(this StringBuilder str)
    {
        return Utf8.GetBytes(str.ToString());
    }

    public static string ToAsciiString(this u8[] bytes)
    {
        return Windows1252.GetString(bytes);
    }

    public static u8[] ToAsciiBytes(this string str)
    {
        return Windows1252.GetBytes(str);
    }

    public static u8[] ToAsciiBytes(this StringBuilder str)
    {
        return Windows1252.GetBytes(str.ToString());
    }

    public static string ToHexString(this IEnumerable<byte> eBytes, string delimiter = "")
    {
        return string.Join(delimiter, eBytes.Select((b) => $"{b:X2}"));
    }

    public static string ToCodeString(this IEnumerable<byte> eBytes, ConsoleId consoleId)
    {
        u8[] bytes = eBytes.ToArray();

        if (consoleId is ConsoleId.Nintendo64 or ConsoleId.GameBoyAdvance)
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

    public static RomString Trim(this RomString oldStr)
    {
        string oldValue = oldStr.Value;
        string trimStart = oldValue.TrimStart();
        string trimEnd = oldValue.TrimEnd();
        string newValue = oldValue.Trim();
        int startDelta = oldValue.Length - trimStart.Length;
        int endDelta = oldValue.Length - trimEnd.Length;
        int lengthDelta = oldValue.Length - newValue.Length;
        var newStr = new RomString(oldStr)
        {
            Value = newValue,
            Addr = new RomRange(oldStr.Addr)
            {
                StartIndex = (u32)(oldStr.Addr.StartIndex - startDelta),
                EndIndex = (u32)(oldStr.Addr.EndIndex - endDelta),
                Length = (u32)(oldStr.Addr.Length - lengthDelta),
                RawBytes = ByteString.CopyFrom(
                    oldStr.Addr.RawBytes
                        .Skip(startDelta)
                        .Take(oldStr.Addr.RawBytes.Length - lengthDelta)
                        .ToArray()
                ),
            },
        };
        return newStr;
    }

    public static RomString WithoutAddress(this RomString rs)
    {
        return new RomString() { Value = rs.Value };
    }

    public static RomString Readable(this RomString oldStr)
    {
        var newStr = new RomString(oldStr).Trim();
        newStr.Value = String.Join(
            "",
            oldStr.Value.Select(c =>
            {
                bool isPrintable = c is >= ' ' and <= '~';
                return isPrintable ? c : ' ';
            })
        );
        return newStr;
    }

    public static string ToDisplayString(this RomRange range)
    {
        return $"[0x{range.StartIndex:X8}, 0x{range.EndIndex - 1:X8}]";
    }

    public static string ToDisplayString(this CodecId codecId)
    {
        return codecId switch
        {
            CodecId.GbGamesharkRom => "GB - GameShark ROM",
            CodecId.GbaGamesharkDatelRom => "GBA - Datel GameShark ROM",
            CodecId.GbaGamesharkFcdRom => "GBA - FCD GameShark ROM",
            CodecId.GbaTvTunerRom => "GBA - TV Tuner ROM",
            CodecId.GbcCodebreakerRom => "GBC - Code Breaker ROM",
            CodecId.GbcGamesharkV3Cdb => "GBC - GameShark v3.x PC cheat DB",
            CodecId.GbcGamesharkV3Gcf => "GBC - GameShark v3.x PC cheat update",
            CodecId.GbcGamesharkV3Rom => "GBC - GameShark v3.x ROM",
            CodecId.GbcGamesharkV4Rom => "GBC - GameShark v4.x ROM",
            CodecId.GbcMonsterbrainRom => "GBC - Monster Brain ROM",
            CodecId.GbcSharkMxRom => "GBC - Shark MX ROM",
            CodecId.GbcXploderRom => "GBC - Xploder/Xplorer ROM",
            CodecId.HammerheadJson => "Hammerhead protobuf JSON",
            CodecId.LibretroText => "Libretro/RetroArch cheat list",
            CodecId.N64Edx7Text => "N64 - EverDrive-64 X7 cheat list",
            CodecId.N64GamesharkMemcard => "N64 - GameShark cheats (mempak note)",
            CodecId.N64GamesharkRom => "N64 - GameShark ROM",
            CodecId.N64GamesharkText => "N64 - Datel-formatted cheat list",
            CodecId.N64GbhunterRom => "N64 - GB Hunter ROM",
            CodecId.N64Pj64V1Text => "N64 - Project 64 v1.6 cheat list",
            CodecId.N64Pj64V3Text => "N64 - Project 64 v3.0 cheat list",
            CodecId.N64Xplorer64Rom => "N64 - Xplorer 64 ROM",
            CodecId.N64Xplorer64Text => "N64 - FCD-formatted cheat list",
            CodecId.OpenemuXml => "OpenEmu XML cheat list",
            CodecId.UnspecifiedCodecId => "UNSPECIFIED ROM format",
            CodecId.UnsupportedCodecId => "UNSUPPORTED ROM format",
            _ => throw new NotSupportedException($"CodecId {codecId} is missing from ToDisplayString()!"),
        };
    }

    public static string FileExtension(this CodecId codecId)
    {
        return codecId switch
        {
            CodecId.GbGamesharkRom => ".gb",
            CodecId.GbaGamesharkDatelRom => ".gba",
            CodecId.GbaGamesharkFcdRom => ".gba",
            CodecId.GbaTvTunerRom => ".gba",
            CodecId.GbcCodebreakerRom => ".gbc",
            CodecId.GbcGamesharkV3Cdb => ".bin",
            CodecId.GbcGamesharkV3Gcf => ".gcf",
            CodecId.GbcGamesharkV3Rom => ".gbc",
            CodecId.GbcGamesharkV4Rom => ".gbc",
            CodecId.GbcMonsterbrainRom => ".gbc",
            CodecId.GbcSharkMxRom => ".gbc",
            CodecId.GbcXploderRom => ".gbc",
            CodecId.HammerheadJson => ".json",
            CodecId.LibretroText => ".txt",
            CodecId.N64Edx7Text => ".txt",
            CodecId.N64GamesharkMemcard => ".n64",
            CodecId.N64GamesharkRom => ".n64",
            CodecId.N64GamesharkText => ".txt",
            CodecId.N64GbhunterRom => ".n64",
            CodecId.N64Pj64V1Text => ".txt",
            CodecId.N64Pj64V3Text => ".txt",
            CodecId.N64Xplorer64Rom => ".n64",
            CodecId.N64Xplorer64Text => ".txt",
            CodecId.OpenemuXml => ".xml",
            CodecId.UnspecifiedCodecId => ".UNSPECIFIED",
            CodecId.UnsupportedCodecId => ".UNSUPPORTED",
            _ => throw new NotSupportedException($"CodecId {codecId} is missing from FileExtension()!"),
        };
    }

    public static string ToDisplayString(this BrandId brandId)
    {
        return brandId switch
        {
            BrandId.ActionReplay => "Action Replay",
            BrandId.Blaze => "Blaze",
            BrandId.Brainboy => "BrainBoy",
            BrandId.CodeBreaker => "Code Breaker",
            BrandId.Equalizer => "Equalizer",
            BrandId.GameBooster => "Game Booster",
            BrandId.GameBuster => "Game Buster",
            BrandId.GameGenie => "Game Genie",
            BrandId.Gameshark => "GameShark",
            BrandId.GbHunter => "GB Hunter",
            BrandId.MonsterBrain => "Monster Brain",
            BrandId.SharkMx => "Shark MX",
            BrandId.Xploder => "Xploder",
            BrandId.Xplorer => "Xplorer",
            BrandId.UnknownBrand => "UNKNOWN brand",
            _ => throw new NotSupportedException($"BrandId {brandId} is missing from ToDisplayString()!"),
        };
    }

    public static string ToDisplayString(this ConsoleId consoleId)
    {
        return consoleId switch
        {
            ConsoleId.GameBoyOriginal => "Game Boy original (GB)",
            ConsoleId.GameBoyColor => "Game Boy Color (GBC)",
            ConsoleId.GameBoyAdvance => "Game Boy Advance (GBA)",
            ConsoleId.GameGear => "Game Gear (GG)",
            ConsoleId.Nintendo64 => "Nintendo 64 (N64)",
            ConsoleId.Playstation1 => "PlayStation 1 (PS/PS1/PSX)",
            ConsoleId.Dreamcast => "Dreamcast (DC)",
            ConsoleId.Gamecube => "GameCube (GC)",
            ConsoleId.Universal => "Universal (all consoles)",
            ConsoleId.UnknownConsole => "UNKNOWN game console",
            _ => throw new NotSupportedException($"ConsoleId {consoleId} is missing from ToDisplayString()!"),
        };
    }

    public static string ToAbbreviation(this ConsoleId consoleId)
    {
        return consoleId switch
        {
            ConsoleId.GameBoyOriginal => "GB",
            ConsoleId.GameBoyColor => "GBC",
            ConsoleId.GameBoyAdvance => "GBA",
            ConsoleId.GameGear => "GG",
            ConsoleId.Nintendo64 => "N64",
            ConsoleId.Playstation1 => "PSX",
            ConsoleId.Dreamcast => "DC",
            ConsoleId.Gamecube => "GC",
            ConsoleId.Universal => "ALL",
            ConsoleId.UnknownConsole => "UNKNOWN",
            _ => throw new NotSupportedException($"ConsoleId {consoleId} is missing from ToDisplayString()!"),
        };
    }

    #endregion

    #region Date/Time

    public static DateTimeOffset GetBuildDate(this Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        return attribute?.DateTimeOffset ?? default(DateTimeOffset);
    }

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
        // 1999-11-24T15:25:52
        return dt.ToString("yyyy-MM-ddTHH:mm:ss");
    }

    public static string ToIsoString(this DateTimeOffset dt)
    {
        // 1999-11-24T15:25:52Z
        return dt.ToString("yyyy-MM-ddTHH:mm:ssK");
    }

    public static string ToFilenameString(this DateTime dt)
    {
        // 19991124T152552
        return dt.ToString("yyyyMMddTHHmmss");
    }

    public static string ToFilenameString(this DateTimeOffset dt)
    {
        // 19991124T152552+0500
        return dt.ToString("yyyyMMddTHHmmss" + dt.Offset.ToUtcOffsetString(false));
    }

    public static string ToUtcOffsetString(this TimeSpan ts, bool separators = true)
    {
        if (separators)
        {
            return "UTC" + ts.Hours switch
            {
                0 => "+00:00",
                > 0 => $"+{ts.Hours:D2}:{ts.Minutes:D2}",
                _ => $"{ts.Hours:D2}:{ts.Minutes:D2}",
            };
        }
        return ts.Hours switch
        {
            0 => "+0000",
            > 0 => $"+{ts.Hours:D2}{ts.Minutes:D2}",
            _ => $"{ts.Hours:D2}{ts.Minutes:D2}",
        };
    }

    public static string ToUtcOffsetString(this Duration duration, bool separators = true)
    {
        return duration.ToTimeSpan().ToUtcOffsetString(separators);
    }

    #endregion

    #region ANSI color codes for terminals

    public static string OrUnknown(this string str, string noun = "")
    {
        bool isUnknown = string.IsNullOrWhiteSpace(str) ||
                         str.ToUpper().Contains("UNKNOWN");
        if (!isUnknown)
        {
            return str;
        }

        str = string.IsNullOrWhiteSpace(str)
            ? "UNKNOWN"
            : str;
        noun = string.IsNullOrWhiteSpace(noun)
            ? ""
            : $" {noun}";
        return $"{str}{noun}";
    }

    #endregion

    #region Strings

    public static string[] SplitLines(this u8[] s)
    {
        return Regex.Split(s.ToAsciiString(), @"\n\r|\r\f|\n");
    }

    public static string[] SplitLines(this string s)
    {
        return Regex.Split(s, @"\r\f|\n");
    }

    public static string ShortenFilePath(this string fullPath)
    {
        string cwd = Environment.CurrentDirectory;
        string homeDir = Environment.GetEnvironmentVariable("userdir") ?? // Windows
                         Environment.GetEnvironmentVariable("HOME") ?? // Unix/Linux
                         "~";

        string shortPath = fullPath
                .Replace(cwd + Path.DirectorySeparatorChar, "")
                .Replace(homeDir, "~")
            ;
        return shortPath;
    }

    public static string ShortName(this FileSystemInfo file)
    {
        return file.FullName.ShortenFilePath();
    }

    public static u8[] HexToBytes(this string hex)
    {
        int len = hex.Length;
        u8[] bytes = new u8[len / 2];
        for (int i = 0; i < len; i += 2)
        {
            string substring = hex.Substring(i, 2);
            bytes[i / 2] = Convert.ToByte(substring, 16);
        }
        return bytes;
    }

    public static ChecksumResult ComputeChecksums(this IEnumerable<u8> bytes)
    {
        u8[] byteArray = bytes.ToArray();
        return new ChecksumResult()
        {
            Crc32Hex = U32ToHexString(Crc32Algorithm.Compute(byteArray)),
            Crc32CHex = U32ToHexString(Crc32CAlgorithm.Compute(byteArray)),
            Md5Hex = BytesToHexString(System.Security.Cryptography.MD5.HashData(byteArray)),
            Sha1Hex = BytesToHexString(System.Security.Cryptography.SHA1.HashData(byteArray)),
        };
    }

    private static string U32ToHexString(u32 checksum)
    {
        return checksum.ToString("X8");
    }

    private static string BytesToHexString(IEnumerable<u8> bytes)
    {
        return string.Join("", bytes.Select((b) => b.ToString("X2")));
    }

    public static RomString ToRomString(this string value)
    {
        return new RomString() { Value = value };
    }

    #endregion

    #region CLI

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

    #endregion
}
