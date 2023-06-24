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
/// made by Datel/InterAct.
/// </summary>
public sealed class GbaGsDatelRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.GameBoyAdvance;
    private const RomFormat ThisRomFormat = RomFormat.GbaGamesharkDatel;

    private const u32 GbaMagicStrAddr = 0x21000;
    private const u32 MinorVersionNumberAddr = 0x21004;
    private const u32 MajorVersionNumberAddr = 0x21005;
    private const u32 GameListAddr = 0x21044;

    private static readonly string[] KnownVersions =
    {
        "v1.0",
        "v3.3",
        "v5.8",
    };

    public GbaGsDatelRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
        ParseVersion();

        RomString firstGameName = Scribe.Seek(GameListAddr).ReadPrintableCString(20, false);
    }

    private void ParseVersion()
    {
        u8 minorVersionNumber = Buffer[MinorVersionNumberAddr];
        u8 majorVersionNumber = Buffer[MajorVersionNumberAddr];
        Metadata.DisplayVersion = $"v{majorVersionNumber}.{minorVersionNumber}";
        Metadata.SortableVersion = Double.Parse($"{majorVersionNumber}.{minorVersionNumber}");
        Metadata.IsKnownVersion = KnownVersions.Contains(Metadata.DisplayVersion);

        RomString versionIdStr =
            Scribe
                .Seek(GbaMagicStrAddr)
                .ReadCStringUntilNull(6, false);
        versionIdStr.Value = string.Join("", versionIdStr.Value.Select((ch) => ch < ' ' ? ch + 0x30 : ch));
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        bool is512KiB = bytes.IsKiB(512);
        return (is256KiB || is512KiB) && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        bool hasMagicNumber = bytes[..4].SequenceEqual(new u8[] { 0x2E, 0x00, 0x00, 0xEA });
        bool hasMagicText = bytes[0x21000..0x21004].ToAsciiString().Equals("GBA_");
        return hasMagicNumber && hasMagicText;
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Format == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    private static BinaryScribe MakeScribe(u8[] rawInput)
    {
        return new LittleEndianScribe(rawInput);
    }

    protected override void PrintCustomHeader()
    {
    }
}
