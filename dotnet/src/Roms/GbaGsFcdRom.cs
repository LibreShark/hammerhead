using System.Globalization;
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
/// GameShark and Action Replay for Game Boy Advance,
/// made by Future Console Design (FCD).
/// </summary>
public sealed class GbaGsFcdRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.GameBoyAdvance;
    private const RomFormat ThisRomFormat = RomFormat.GbaGamesharkFcd;

    public GbaGsFcdRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
        ParseVersion();
    }

    private void ParseVersion()
    {
        s32 gsAddr = Buffer.Find("GameShark");
        s32 cbAddr = Buffer.Find("CodeBreaker");
        s32 xpAddr = Buffer.Find("XploderAdv");
        u32 brandAddr;
        if (gsAddr > -1)
        {
            brandAddr = (u32)gsAddr;
            Metadata.Brand = RomBrand.Gameshark;
        }
        else if (cbAddr > -1)
        {
            brandAddr = (u32)cbAddr;
            Metadata.Brand = RomBrand.CodeBreaker;
        }
        else if (xpAddr > -1)
        {
            brandAddr = (u32)xpAddr;
            Metadata.Brand = RomBrand.Xploder;
        }
        else
        {
            return;
        }

        u32 countryAddr = brandAddr - 0x10;
        u32 variantAddr = brandAddr + 0x10;

        RomString countryStr = Scribe.Seek(countryAddr).ReadPrintableCString();
        RomString brandStr = Scribe.Seek(brandAddr).ReadPrintableCString();
        RomString variantStr = Scribe.Seek(variantAddr).ReadPrintableCString();

        u32 dateTimeAddr = variantStr.Addr.EndIndex + 0xF;
        Scribe.Seek(dateTimeAddr);
        while (Scribe.PeekBytes(1)[0] == 0)
        {
            Scribe.Skip(1);
        }
        RomString dateTimeStr = Scribe.ReadPrintableCString();

        Metadata.Identifiers.Add(countryStr);
        Metadata.Identifiers.Add(brandStr);
        Metadata.Identifiers.Add(variantStr);
        Metadata.Identifiers.Add(dateTimeStr);

        if (countryStr.Value is "USA" or "US of A")
        {
            Metadata.LanguageIetfCode = "en-US";
        }
        else if (countryStr.Value is "UK")
        {
            Metadata.LanguageIetfCode = "en-GB";
        }

        // E.g.: "Fri Nov 15 13:55:45 2002"
        DateTime dateTime = DateTime.ParseExact(dateTimeStr.Value, "ddd MMM d H:mm:ss yyyy", DateTimeFormatInfo.InvariantInfo);
        Metadata.BuildDateRaw = dateTimeStr;
        Metadata.BuildDateIso = dateTime.ToIsoString();

        // TODO(CheatoBaggins): Determine original time zone
        // Metadata.BuildDateProto = Timestamp.FromDateTime(dateTime);

        // TODO(CheatoBaggins): Implement
        Metadata.IsKnownVersion = false;
    }

    public static bool Is(u8[] bytes)
    {
        // gba-cblite-r1-prototype-SST39VF100-20011019.bin
        // gba-gspro-sp-madcatz-SST39VF100-20030821.bin
        bool is128KiB = bytes.IsKiB(128);

        // gba-gspro-sp-karabiner-SST39VF800ATSOP48-20060614.bin
        bool is1024KiB = bytes.IsMiB(1);

        return (is128KiB || is1024KiB) && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        bool isMagicNumberMatch = bytes[..4].SequenceEqual(new u8[] { 0x2E, 0x00, 0x00, 0xEA });
        bool isCopyrightMatch = bytes[0x05..0x20].ToAsciiString() == "(C) Future Console Design *";
        bool isFcdFcdFcdMatch = bytes[0x80..0xA0].ToAsciiString() == "FCDFCDFCDFCDFCD!FCDFCDFCDFCDFCD!";
        return isMagicNumberMatch && isCopyrightMatch && isFcdFcdFcdMatch;
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Format == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    private static BinaryScribe MakeScribe(u8[] bytes)
    {
        return new LittleEndianScribe(bytes);
    }

    protected override void PrintCustomHeader()
    {
    }
}
