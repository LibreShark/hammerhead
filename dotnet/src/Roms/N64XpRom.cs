namespace LibreShark.Hammerhead;

public class N64XpRom : Rom
{
    private const RomType ThisRomType = RomType.N64Xplorer64;

    public N64XpRom(string filePath, byte[] bytes)
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
        byte[] maybeScrambledFcdBytes =
        {
            bytes[0x131E], bytes[0x131F],
            bytes[0x133E], bytes[0x133F],
            bytes[0x171E], bytes[0x171F],
            bytes[0x167E], bytes[0x167F],
            bytes[0x031E], bytes[0x031F],
            bytes[0x033E], bytes[0x033F],
            bytes[0x071E], bytes[0x071F],
            bytes[0x073E], bytes[0x073F],
            bytes[0x139E], bytes[0x139F],
            bytes[0x13BE], bytes[0x13BF],
            bytes[0x179E],
        };
        byte[] expectedFcdBytes = "FUTURE CONSOLE DESIGN"u8.ToArray();
        bool isFirstEqual = expectedFcdBytes[0x00..0x05]
            .SequenceEqual(maybeScrambledFcdBytes[0x00..0x05]);
        bool isSecondEqual = expectedFcdBytes[0x08..0x14]
            .SequenceEqual(maybeScrambledFcdBytes[0x08..0x14]);
        return isFirstEqual && isSecondEqual;
    }

    private static bool DetectEncrypted(byte[] bytes)
    {
        // TODO(CheatoBaggins): Implement
        return false;
    }

    private static bool DetectUnobfuscated(byte[] bytes)
    {
        var b = bytes[0x40..0x55];
        var s = System.Text.Encoding.UTF8.GetString(b);
        return s == "FUTURE CONSOLE DESIGN";
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        return is256KiB && (DetectScrambled(bytes) || DetectEncrypted(bytes) || DetectUnobfuscated(bytes));
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
        Console.WriteLine($"N64 Xplorer 64 ROM file: '{FilePath}'");
        Console.WriteLine($"Encrypted: {IsEncrypted}");
        Console.WriteLine($"Scrambled: {IsScrambled}");
    }
}
