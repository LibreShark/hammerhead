namespace LibreShark.Hammerhead;

public class N64GsRom : Rom
{
    private const RomType ThisRomType = RomType.N64Gameshark;

    public N64GsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
        IsEncrypted = DetectEncrypted(bytes);

        // Not applicable to GameShark ROMs. Only Xplorer 64 scrambles bytes.
        IsScrambled = false;
    }

    private static bool DetectEncrypted(byte[] bytes)
    {
        // TODO(CheatoBaggins): Implement
        return false;
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        byte[] first4Bytes = bytes[..4];
        return is256KiB &&
               (first4Bytes.SequenceEqual(new byte[] { 0x80, 0x37, 0x12, 0x40 }) ||
                first4Bytes.SequenceEqual(new byte[] { 0x80, 0x37, 0x12, 0x00 }));
    }

    public static bool Is(Rom rom)
    {
        return rom.Type == ThisRomType;
    }

    public static bool Is(RomType type)
    {
        return type == ThisRomType;
    }

    public override void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"N64 GameShark ROM file: '{FilePath}'");
        Console.WriteLine($"Encrypted: {IsEncrypted}");
    }
}
