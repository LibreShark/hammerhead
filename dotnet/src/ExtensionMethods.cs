using System.Text;
using Google.Protobuf;
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
    #region Bytes

    private static readonly Encoding Ascii = Encoding.GetEncoding(
        "ascii",
        new EncoderReplacementFallback("?"),
        new DecoderReplacementFallback("?"));

    public static string ToAsciiString(this byte[] bytes)
    {
        return Ascii.GetString(bytes);
    }

    public static string ToHexString(this IEnumerable<byte> eBytes, string delimiter = "")
    {
        return string.Join(delimiter, eBytes.Select((b) => $"{b:X2}"));
    }

    public static string ToCodeString(this IEnumerable<byte> eBytes, GameConsole console)
    {
        byte[] bytes = eBytes.ToArray();

        if (console == GameConsole.Nintendo64)
        {
            return $"{bytes[..4].ToHexString()} {bytes[4..].ToHexString()}";
        }

        return bytes.ToHexString();
    }

    public static bool Contains(this byte[] haystackBytes, string needleStr)
    {
        return haystackBytes.Find(needleStr) > -1;
    }

    public static bool Contains(this byte[] haystackBytes, byte[] needleBytes)
    {
        return haystackBytes.Find(needleBytes) > -1;
    }

    public static s32 Find(this byte[] haystackBytes, string needleStr)
    {
        return Find(haystackBytes, Encoding.UTF8.GetBytes(needleStr));
    }

    public static s32 Find(this byte[] haystackBytes, byte[] needleBytes)
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

    public static bool IsKiB(this byte[] bytes, int numKiB)
    {
        return bytes.Length == numKiB * 1024;
    }

    public static bool IsMiB(this byte[] bytes, int numMiB)
    {
        return bytes.Length == numMiB * 1024 * 1024;
    }

    public static bool IsPadding(this IEnumerable<byte> eBytes)
    {
        return eBytes.All((b) => b is 0x00 or 0xFF or 0xAA);
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
        byte[] oldBytes = oldRS.Addr.RawBytes.ToByteArray();
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

    #endregion
}
