using System.Text.Json;
using Force.Crc32;

namespace LibreShark.Hammerhead.IO;

public class Checksum
{
    // These fields MUST be getters/setters in order to be serialized by
    // JsonSerializer.Serialize(this).
    public string Crc32Hex { get; private set; }
    public string Crc32CHex { get; private set; }
    public string Md5Hex { get; private set; }
    public string Sha1Hex { get; private set; }

    public static Checksum From(IEnumerable<byte> bytes)
    {
        return new Checksum(bytes.ToArray());
    }

    private Checksum(byte[] bytes)
    {
        Crc32Hex = U32ToString(Crc32Algorithm.Compute(bytes));
        Crc32CHex = U32ToString(Crc32CAlgorithm.Compute(bytes));
        Md5Hex = BytesToString(System.Security.Cryptography.MD5.HashData(bytes));
        Sha1Hex = BytesToString(System.Security.Cryptography.SHA1.HashData(bytes));
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
