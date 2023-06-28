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
/// TV Tuner for Game Boy Advance, made by Blaze and Pelican Accessories.
/// There are NTSC and PAL variants.
/// </summary>
public sealed class GbaTvTunerRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.GameBoyAdvance;
    private const RomFormat ThisRomFormat = RomFormat.GbaTvTuner;

    public GbaTvTunerRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
    }

    public override bool FormatSupportsCustomCheatCodes()
    {
        return false;
    }

    public static bool Is(u8[] bytes)
    {
        bool is512KiB = bytes.IsKiB(512);
        bool is16MiB = bytes.IsMiB(16);
        return (is512KiB || is16MiB) && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        var idBytes = bytes[0xA0..0xAB];
        var idStr = idBytes.ToAsciiString();
        return idStr == "GBA_Capture";
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.RomFormat == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    private static BinaryScribe MakeScribe(u8[] bytes)
    {
        return new LittleEndianScribe(bytes);
    }
}
