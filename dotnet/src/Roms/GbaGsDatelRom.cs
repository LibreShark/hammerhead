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

    public GbaGsDatelRom(string filePath, u8[] rawInput)
        : base(filePath, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
        var minorVersionNumber = Buffer[0x21004];
        var majorVersionNumber = Buffer[0x21005];
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        return is256KiB && Detect(bytes);
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
