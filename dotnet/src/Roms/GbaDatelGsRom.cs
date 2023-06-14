namespace LibreShark.Hammerhead;

// TODO(CheatoBaggins): Confirm whether this is actually made by Datel or FCD.
public class GbaDatelGsRom : Rom
{
    private const RomType ThisRomType = RomType.GbaFcdGameshark;

    public GbaDatelGsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
        IsEncrypted = false;
        IsScrambled = false;
    }

    public static bool Is(byte[] bytes)
    {
        byte[] first4Bytes = bytes[..4];
        bool is256KiB = bytes.Length == 0x00040000;
        bool isMagicNumberMatch = first4Bytes.SequenceEqual(new byte[] { 0x2E, 0x00, 0x00, 0xEA });
        bool hasMagicText = bytes.Contains("Press START to Play Game");
        return is256KiB && isMagicNumberMatch && hasMagicText;
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
        Console.WriteLine($"GBA Datel GameShark ROM file: '{FilePath}'");
    }
}
