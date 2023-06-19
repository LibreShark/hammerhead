using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Roms;

/// <summary>
/// GameShark and Action Replay for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcGsRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.GbcGameshark;

    // TODO(CheatoBaggins): Create LittleEndianReader/Writer
    private readonly BigEndianReader _reader;
    private readonly BigEndianWriter _writer;

    public GbcGsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
        _reader = new BigEndianReader(Bytes);
        _writer = new BigEndianWriter(Bytes);

        Metadata.Brand = IsGs(Bytes) ? RomBrand.Gameshark : IsAr(Bytes) ? RomBrand.ActionReplay : RomBrand.UnknownBrand;
        Metadata.SortableVersion = ReadVersionNumber();
        Metadata.DisplayVersion = $"v{Metadata.SortableVersion:F2}";

        Metadata.LanguageIetfCode = Metadata.Brand switch
        {
            RomBrand.Gameshark => "en-US",
            RomBrand.ActionReplay => "en-GB",
            _ => "und",
        };

        RomString title = _reader.Seek(0x0134).ReadCString();
        Metadata.Identifiers.Add(title);
    }

    public static bool Is(byte[] bytes)
    {
        bool is128KiB = bytes.Length == 0x00020000;
        return is128KiB && Detect(bytes);
    }

    private static bool Detect(byte[] bytes)
    {
        bool hasMagicNumber = bytes[..4].SequenceEqual(new byte[] { 0xC3, 0x50, 0x01, 0x78 });
        bool hasIdentifier = IsGs(bytes) || IsAr(bytes);
        return hasMagicNumber && hasIdentifier;
    }

    private static bool IsGs(byte[] bytes)
    {
        string identifier = bytes[0x0134..0x0146].ToAsciiString();
        return identifier.StartsWith("Gameshark     V");
    }

    private static bool IsAr(byte[] bytes)
    {
        string identifier = bytes[0x0134..0x0146].ToAsciiString();
        return identifier.StartsWith("Action Replay V");
    }

    private double ReadVersionNumber()
    {
        RomString verNumRaw = _reader.Seek(0x0143).ReadPrintableCString();
        if (Double.TryParse(verNumRaw.Value, out double verNumParsed))
        {
            return verNumParsed;
        }
        return -1;
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
    }
}
