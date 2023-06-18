using System.Text;

namespace LibreShark.Hammerhead;

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

    public static string ToHexString(this byte[] bytes, string delimiter = "")
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

    public static int Find(this byte[] haystackBytes, string needleStr)
    {
        return Find(haystackBytes, Encoding.UTF8.GetBytes(needleStr));
    }

    public static int Find(this byte[] haystackBytes, byte[] needleBytes)
    {
        int needleLen = needleBytes.Length;
        int haystackLen = haystackBytes.Length;
        for (int i = 0; i < haystackLen - needleLen; i++)
        {
            int end = i + needleLen;
            if (haystackBytes[i..end].SequenceEqual(needleBytes))
            {
                return i;
            }
        }

        return -1;
    }

    public static string ToDisplayString(this RomFormat format)
    {
        switch (format)
        {
            case RomFormat.N64Gameshark:
                return "N64 GameShark ROM format";
            case RomFormat.N64Gbhunter:
                return "N64 GB Hunter ROM format";
            case RomFormat.N64Xplorer64:
                return "N64 Xplorer 64 ROM format";
            default:
                // TODO(CheatoBaggins): Implement all
                return "UNKNOWN";
        }
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
                return "Xploder/Xplorer";
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

    public static RomString Trim(this RomString oldRomString)
    {
        return new RomString(oldRomString)
        {
            Value = oldRomString.Value.Trim(),
        };
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
}
