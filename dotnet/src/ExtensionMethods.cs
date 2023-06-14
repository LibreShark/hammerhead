namespace LibreShark.Hammerhead;

public static class ExtensionMethods
{
    public static string ToUtf8String(this byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    public static bool Contains(this byte[] haystackBytes, string needleStr)
    {
        return haystackBytes.Find(needleStr) > -1;
    }

    public static int Find(this byte[] haystackBytes, string needleStr)
    {
        byte[] needleBytes = System.Text.Encoding.UTF8.GetBytes(needleStr);
        int needleLen = needleBytes.Length;
        int haystackLen = haystackBytes.Length;
        for (int i = 0; i < haystackLen - needleLen - 1; i++)
        {
            int end = i + needleLen;
            if (haystackBytes[i..end].SequenceEqual(needleBytes))
            {
                return i;
            }
        }

        return -1;
    }
}
