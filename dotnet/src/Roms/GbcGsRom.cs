using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.N64;

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

    private readonly List<RawGbcGsCheat> _rawCheats = new();

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
        byte[] unknownBytes1 = _reader.ReadBytes(2);
        ReadCheats();
        byte[] unknownBytes2 = _reader.ReadBytes(2);
        ReadCheats();

        PrintRawCheats();
    }

    private void ReadGames()
    {
        _reader.Seek(GameListAddr);
        u8[] unknownBytes1 = _reader.ReadBytes(2);
        // The number 455 is hard-coded, and space is always pre-allocated in the ROM file
        for (u16 i = 0; i < 455; i++)
        {
            u8[] unknownBytes2 = _reader.ReadBytes(2);
            RomString gameName = _reader.ReadPrintableCString(15).Trim();
            if (gameName.Value.Length == 0)
            {
                continue;
            }

            Games.Add(new Game { Name = gameName.Value });
        }
    }

    private void ReadCheats()
    {
        // this number is hard-coded and pre-allocated in the ROM file
        for (u16 i = 0; i < 455; i++)
        {
            // TODO(CheatoBaggins): Little endian
            byte[] code = _reader.ReadBytes(4);
            RomString cheatName = _reader.ReadPrintableCString(12).Trim();

            // TODO(CheatoBaggins): Figure out why this hack is needed and fix it
            _reader.Seek(_reader.Position - 1);

            byte[] unknownBytes = _reader.ReadBytes(2);
            if (cheatName.Value.Length == 0)
            {
                continue;
            }
            _rawCheats.Add(new RawGbcGsCheat(code, cheatName, unknownBytes));
        }
    }

    private void PrintRawCheats()
    {
        RawGbcGsCheat[] rawCheats = _rawCheats.ToArray();
        for (int i = 0; i < rawCheats.Length; i++)
        {
            var cheat = rawCheats[i];
            var code = cheat.Code;
            var unknownBytes = cheat.UnknownBytes;
            var cheatName = cheat.Name;
            string codeStr = code.ToHexString();
            string unknownStr = unknownBytes.ToHexString();
            Console.WriteLine(
                $"{cheatName.Addr.ToDisplayString()} cheat[{i:D4}] = {codeStr} / {unknownStr} = {cheatName.Value}");
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

    private class RawGbcGsCheat
    {
        public readonly byte[] Code;
        public readonly RomString Name;
        public readonly byte[] UnknownBytes;

        public RawGbcGsCheat(byte[] code, RomString name, byte[] unknownBytes)
        {
            Code = code;
            Name = name;
            UnknownBytes = unknownBytes;
        }
    }
}
