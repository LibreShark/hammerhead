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

public sealed class UnknownRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.UnknownGameConsole;
    private const RomFormat ThisRomFormat = RomFormat.UnknownRomFormat;

    public UnknownRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
    }

    public override bool FormatSupportsCustomCheatCodes()
    {
        return false;
    }

    private static BinaryScribe MakeScribe(u8[] rawInput)
    {
        return new BigEndianScribe(rawInput);
    }
}
