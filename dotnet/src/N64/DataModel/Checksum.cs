using System.Text.Json;
using Force.Crc32;

namespace LibreShark.Hammerhead.N64;

public class Checksum
{
    public string Crc32 { get; set; }
    public string Crc32C { get; set; }
    public string MD5 { get; set; }
    public string SHA1 { get; set; }

    public static Checksum From(byte[] bytes)
    {
        return new Checksum(bytes);
    }

    private Checksum(byte[] bytes)
    {
        Crc32 = ToString(Crc32Algorithm.Compute(bytes));
        Crc32C = ToString(Crc32CAlgorithm.Compute(bytes));
        MD5 = ToString(System.Security.Cryptography.MD5.HashData(bytes));
        SHA1 = ToString(System.Security.Cryptography.SHA1.HashData(bytes));
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    private static string ToString(uint checksum)
    {
        return checksum.ToString("X8");
    }

    private static string ToString(byte[] bytes)
    {
        return string.Join("", bytes.Select((b) => b.ToString("X2")));
    }
}
