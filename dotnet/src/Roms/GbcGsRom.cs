namespace LibreShark.Hammerhead.Roms;

/// <summary>
/// GameShark and Action Replay for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcGsRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.GbcGameshark;

    public GbcGsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
    }

    public static bool Is(byte[] bytes)
    {
        bool is128KiB = bytes.Length == 0x00020000;
        return is128KiB && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        bool hasMagicNumber = bytes[..4].SequenceEqual(new byte[] { 0xC3, 0x50, 0x01, 0x78 });

        string identifier = bytes[0x0134..0x0146].ToAsciiString();
        bool hasIdentifier = identifier.StartsWith("Gameshark     V") ||
                             identifier.StartsWith("Action Replay V");

        return hasMagicNumber && hasIdentifier;
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Format == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    protected override void PrintCustomHeader()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"GBC GameShark ROM file: '{Metadata.FilePath}'");
    }
}
