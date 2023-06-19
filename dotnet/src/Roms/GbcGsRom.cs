using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Roms;

// ReSharper disable BuiltInTypeReferenceStyle
using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using s64 = Int64;
using u64 = UInt64;
using f64 = Double;

/// <summary>
/// GameShark and Action Replay for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcGsRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.GbcGameshark;
    private const u32 TitleAddr = 0x00000134;
    private const u32 VerNumAddr = 0x00000143;
    private const u32 GameListAddr = 0x00008000;
    private const u32 CheatListAddr = 0x0000A000;

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

        RomString title = _reader.Seek(TitleAddr).ReadCString();
        Metadata.Identifiers.Add(title);

        ReadGames();
        ReadCheats();
    }

    private void ReadGames()
    {
        _reader.Seek(GameListAddr);
        u8[] unknownBytes1 = _reader.ReadBytes(2);
        u16 gameCount = 0;
        // The number 455 is hard-coded, and space is always pre-allocated in the ROM file
        for (u16 i = 0; i < 455; i++)
        {
            // TODO(CheatoBaggins): Little endian
            u8[] unknownBytes2 = _reader.ReadBytes(2);
            RomString gameName = _reader.ReadPrintableCString(15).Trim();
            if (gameName.Value.Length == 0)
            {
                continue;
            }

            Console.WriteLine($"{gameName.Addr.ToDisplayString()} game[{gameCount:D3}] = {gameName.Value}");

            gameCount++;
        }
    }

    private void ReadCheats()
    {
        _reader.Seek(CheatListAddr);
        // this number is hard-coded and pre-allocated in the ROM file
        u8[] unknownBytes1 = _reader.ReadBytes(2);
        u16 gameCount = 0;
        for (u16 i = 0; i < 455; i++)
        {
            // TODO(CheatoBaggins): Little endian
            u8[] unknownBytes2 = _reader.ReadBytes(2);
            RomString gameName = _reader.ReadPrintableCString(15).Trim();
            if (gameName.Value.Length == 0)
            {
                continue;
            }

            Console.WriteLine($"{gameName.Addr.ToDisplayString()} game[{gameCount:D3}] = {gameName.Value}");

            gameCount++;
        }
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
        string identifier = bytes[(int)TitleAddr..(int)(TitleAddr + 18)].ToAsciiString();
        return identifier.StartsWith("Gameshark     V");
    }

    private static bool IsAr(byte[] bytes)
    {
        string identifier = bytes[(int)TitleAddr..(int)(TitleAddr + 18)].ToAsciiString();
        return identifier.StartsWith("Action Replay V");
    }

    private double ReadVersionNumber()
    {
        RomString verNumRaw = _reader.Seek(VerNumAddr).ReadPrintableCString();
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
