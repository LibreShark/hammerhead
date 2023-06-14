namespace LibreShark.Hammerhead;

public class GbaFcdGsRom : Rom
{
    private const RomType ThisRomType = RomType.GbaFcdGameshark;

    public GbaFcdGsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
        IsEncrypted = DetectEncrypted(bytes);
        IsScrambled = DetectScrambled(bytes);

        if (IsEncrypted)
        {
            Decrypt();
        }
        else if (IsScrambled)
        {
            Unscramble();
        }
    }

    private void Decrypt()
    {
        // TODO(CheatoBaggins): Implement
    }

    private void Unscramble()
    {
        // TODO(RWeick): Implement
    }

    private static bool DetectScrambled(byte[] bytes)
    {
        // TODO(CheatoBaggins): Implement
        return false;
    }

    private static bool DetectEncrypted(byte[] bytes)
    {
        // TODO(CheatoBaggins): Implement
        return false;
    }

    private static bool DetectUnobfuscated(byte[] bytes)
    {
        byte[] first4Bytes = bytes[..4];
        bool isMagicNumberMatch = first4Bytes.SequenceEqual(new byte[] { 0x2E, 0x00, 0x00, 0xEA });
        bool isCopyrightMatch = bytes[0x05..0x20].ToUtf8String() == "(C) Future Console Design *";
        bool isFcdFcdFcdMatch = bytes[0x80..0xA0].ToUtf8String() == "FCDFCDFCDFCDFCD!FCDFCDFCDFCDFCD!";
        return isMagicNumberMatch && isCopyrightMatch && isFcdFcdFcdMatch;
    }

    public static bool Is(byte[] bytes)
    {
        // gba-cblite-r1-prototype-SST39VF100-20011019.bin
        // gba-gspro-sp-madcatz-SST39VF100-20030821.bin
        bool is128KiB = bytes.Length == 0x00020000;

        // gba-gspro-sp-karabiner-SST39VF800ATSOP48-20060614.bin
        bool is1024KiB = bytes.Length == 0x00100000;

        return (is128KiB || is1024KiB)
               && (DetectScrambled(bytes) || DetectEncrypted(bytes) || DetectUnobfuscated(bytes));
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
        Console.WriteLine($"GBA FCD GameShark ROM file: '{FilePath}'");
    }
}
