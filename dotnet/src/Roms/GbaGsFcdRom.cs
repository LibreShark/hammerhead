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
