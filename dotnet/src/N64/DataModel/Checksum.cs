using System.Text.Json;
using Force.Crc32;

namespace LibreShark.Hammerhead.N64;

public class Checksum
{
    public string CRC32 { get; private set; }
    public string CRC32C { get; private set; }
    public string MD5 { get; private set; }
    public string SHA1 { get; private set; }

    public static Checksum From(byte[] bytes)
    {
        return new Checksum(bytes);
    }

    private Checksum(byte[] bytes)
    {
        CRC32 = U32ToString(Crc32Algorithm.Compute(bytes));
        CRC32C = U32ToString(Crc32CAlgorithm.Compute(bytes));
        MD5 = BytesToString(System.Security.Cryptography.MD5.HashData(bytes));
        SHA1 = BytesToString(System.Security.Cryptography.SHA1.HashData(bytes));
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    private static string U32ToString(uint checksum)
    {
        return checksum.ToString("X8");
    }

    private static string BytesToString(IEnumerable<byte> bytes)
    {
        return string.Join("", bytes.Select((b) => b.ToString("X2")));
    }
}
