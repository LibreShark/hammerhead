namespace LibreShark.Hammerhead;

/// <summary>
/// GameShark (USA/CAN), Action Replay (UK/EU), Equalizer (UK/EU), and Game Buster (Germany) for
/// Nintendo 64, made by Datel/InterAct.
/// </summary>
public sealed class N64GsRom : Rom
{
    private const RomType ThisRomType = RomType.N64Gameshark;

    public N64GsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
        if (IsEncrypted())
        {
            Decrypt();
        }

        // TODO(CheatoBaggins): Decompress v3.x ROM files
    }

    public override bool IsEncrypted()
    {
        return DetectEncrypted(InitialBytes.ToArray());
    }

    private void Decrypt()
    {
        if (!DetectEncrypted(Bytes))
        {
            return;
        }
        // TODO(CheatoBaggins): Implement
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        return is256KiB && (DetectDecrypted(bytes) || DetectEncrypted(bytes));
    }

    private static bool DetectDecrypted(byte[] bytes)
    {
        byte[] first4Bytes = bytes[..4];
        return first4Bytes.SequenceEqual(new byte[] { 0x80, 0x37, 0x12, 0x40 }) ||
               first4Bytes.SequenceEqual(new byte[] { 0x80, 0x37, 0x12, 0x00 });
    }

    private static bool DetectEncrypted(byte[] bytes)
    {
        // TODO(CheatoBaggins): Implement
        return false;
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
        Console.WriteLine($"Encrypted: {IsEncrypted()}");
    }
}
