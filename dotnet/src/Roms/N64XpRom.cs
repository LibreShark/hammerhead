namespace LibreShark.Hammerhead;

/// <summary>
/// Xplorer 64 for Nintendo 64,
/// made by Blaze and Future Console Design (FCD).
/// </summary>
public sealed class N64XpRom : Rom
{
    private const RomType ThisRomType = RomType.N64Xplorer64;

    public N64XpRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
        if (IsScrambled())
        {
            Unscramble();
        }
    }

    public override bool IsScrambled()
    {
        return DetectScrambled(InitialBytes.ToArray());
    }

    private void Unscramble()
    {
        // TODO(RWeick): Implement
    }

    public byte[] GetPlain()
    {
        // Return a copy of the array to prevent the caller from mutating
        // internal state.
        return Bytes.ToArray();
    }

    public byte[] GetScrambled()
    {
        // TODO(RWeick): Implement
        return new byte[] {};
    }

    private static bool DetectPlain(byte[] bytes)
    {
        var idBytes = bytes[0x40..0x55];
        var idStr = idBytes.ToUtf8String();
        return idStr == "FUTURE CONSOLE DESIGN";
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

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        return is256KiB && (DetectPlain(bytes) || DetectScrambled(bytes));
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Type == ThisRomType;
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
        Console.WriteLine($"N64 Xplorer 64 ROM file: '{Metadata.FilePath}'");
        Console.WriteLine($"Scrambled: {IsScrambled()}");
    }
}