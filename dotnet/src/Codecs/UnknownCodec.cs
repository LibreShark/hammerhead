using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Codecs;

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

public sealed class UnknownCodec : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.UnknownConsole;
    private const CodecId ThisCodecId = CodecId.UnsupportedCodecId;

    public UnknownCodec(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
    }

    private static AbstractBinaryScribe MakeScribe(u8[] rawInput)
    {
        return new BigEndianScribe(rawInput);
    }
}
