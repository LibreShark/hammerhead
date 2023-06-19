using System.Text;
using Google.Protobuf;

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
    private static readonly Encoding Ascii = Encoding.GetEncoding(
        "ascii",
        new EncoderReplacementFallback("?"),
        new DecoderReplacementFallback("?"));

    public static string ToAsciiString(this byte[] bytes)
    {
        return Ascii.GetString(bytes);
    }

    public static string ToHexString(this IEnumerable<byte> bytes, string delimiter = "")
    {
        return string.Join(delimiter, bytes.Select((b) => $"{b:X2}"));
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

    public static RomString Trim(this RomString oldRS)
    {
        RomString newRS = new RomString
        {
            Value = oldRS.Value.Trim(),
        };
        u32 len = (u32)newRS.Value.Length;
        u32 startIndex = oldRS.Addr.StartIndex;
        byte[] oldBytes = oldRS.Addr.RawBytes.ToByteArray();
        newRS.Addr = new RomRange
        {
            StartIndex = startIndex,
            EndIndex = startIndex + len,
            Length = len,
            RawBytes = ByteString.CopyFrom(oldBytes[..(int)len]),
        };
        return newRS;
    }

    public static string ToDisplayString(this RomFormat format)
    {
        return format switch
        {
            RomFormat.N64Gameshark => "N64 GameShark ROM format",
            RomFormat.N64Gbhunter => "N64 GB Hunter ROM format",
            RomFormat.N64Xplorer64 => "N64 Xplorer 64 ROM format",
            RomFormat.GbcGameshark => "GBC GameShark ROM format",
            RomFormat.GbcXploder => "GBC Xploder/Xplorer ROM format",
            RomFormat.GbcCodebreaker => "GBC Code Breaker ROM format",
            RomFormat.GbcMonsterbrain => "GBC Monster Brain ROM format",
            RomFormat.GbcSharkMx => "GBC Shark MX ROM format",
            _ => "MISSING FROM RomFormat.ToDisplayString()!",
        };
    }

    public static string ToDisplayString(this RomBrand brand)
    {
        switch (brand)
        {
            case RomBrand.Gameshark:
                return "GameShark";
            case RomBrand.ActionReplay:
                return "Action Replay";
            case RomBrand.Equalizer:
                return "Equalizer";
            case RomBrand.GameBuster:
                return "Game Buster";
            case RomBrand.Xploder:
                return "Xploder";
            case RomBrand.Xplorer:
                return "Xplorer";
            case RomBrand.CodeBreaker:
                return "Code Breaker";
            case RomBrand.GbHunter:
                return "GB Hunter";
            case RomBrand.GameBooster:
                return "Game Booster";
            case RomBrand.GameGenie:
                return "Game Genie";
            case RomBrand.SharkMx:
                return "Shark MX";
            default:
                // TODO(CheatoBaggins): Implement all
                return "UNKNOWN";
        }
    }

    public static string ToDisplayString(this RomRange range)
    {
        return $"[0x{range.StartIndex:X8}, 0x{range.EndIndex - 1:X8}]";
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

    public static string ToN64CodeString(this byte[] code)
    {
        return code[..4].ToHexString() + " " + code[4..].ToHexString();
    }

    public static bool EqualsIgnoreCase(this string str1, string str2)
    {
        return String.Compare(str1, str2, StringComparison.InvariantCultureIgnoreCase) == 0;
    }
}
