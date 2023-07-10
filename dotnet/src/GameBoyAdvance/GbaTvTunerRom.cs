using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.GameBoyAdvance;

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

    public override ICodec WriteChangesToBuffer()
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

    public static bool Is(ICodec codec)
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
