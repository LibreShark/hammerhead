namespace LibreShark.Hammerhead;

public class GbcGsRom : Rom
{
    private const RomType ThisRomType = RomType.GbcGameshark;

    public GbcGsRom(string filePath, byte[] bytes)
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
        bool is128KiB = bytes.Length == 0x00020000;
        byte[] first4Bytes = bytes[..4];
        string identifier = bytes[0x0134..0x0146].ToUtf8String();
        return is128KiB &&
               first4Bytes.SequenceEqual(new byte[] { 0xC3, 0x50, 0x01, 0x78 }) &&
               (identifier.StartsWith("Gameshark     V") ||
                identifier.StartsWith("Action Replay V"));
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
        Console.WriteLine($"GBC GameShark ROM file: '{FilePath}'");
    }
}
