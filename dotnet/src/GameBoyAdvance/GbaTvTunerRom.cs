using LibreShark.Hammerhead.Nintendo64;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.GameBoyAdvance;

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
public sealed class GbaTvTunerRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyAdvance;
    private const CodecId ThisCodecId = CodecId.GbaTvTunerRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static GbaTvTunerRom Create(string filePath, u8[] rawInput)
    {
        return new GbaTvTunerRom(filePath, rawInput);
    }

    public override CodecId DefaultCheatOutputCodec => CodecId.UnsupportedCodecId;

    private GbaTvTunerRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsFirmware = true;
        Support.HasFirmware = true;

        // TODO(CheatoBaggins): Detect
        Support.SupportsUserPrefs = true;
        Support.HasPristineUserPrefs = false;
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
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

    public static bool Is(AbstractCodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }

    private static AbstractBinaryScribe MakeScribe(u8[] bytes)
    {
        return new LittleEndianScribe(bytes);
    }
}
