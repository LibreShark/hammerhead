using System.Text;

namespace LibreShark.Hammerhead;

public static class ExtensionMethods
{
    public static string ToUtf8String(this byte[] bytes)
    {
        var utf8 = Encoding.GetEncoding(
            "utf-8",
            new EncoderReplacementFallback("?"),
            new DecoderReplacementFallback("?"));
        return utf8.GetString(bytes);
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

    public static string ToDisplayString(this RomType type)
    {
        switch (type)
        {
            case RomType.N64Gameshark:
                return "N64 GameShark";
            case RomType.N64Gbhunter:
                return "N64 GB Hunter";
            case RomType.N64Xplorer64:
                return "N64 Xplorer 64";
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
            default:
                // TODO(CheatoBaggins): Implement all
                return "UNKNOWN";
        }
    }

    public static RomString Trim(this RomString oldRomString)
    {
        var newRomString = new RomString(oldRomString);
        newRomString.Value = oldRomString.Value.Trim();
        return newRomString;
    }

    public static string ToDisplayString(this RomRange range)
    {
        return $"[0x{range.StartIndex:X8}, 0x{range.EndIndex - 1:X8}]";
    }
}
