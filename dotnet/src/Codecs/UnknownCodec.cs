using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Nintendo64;

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

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static UnknownCodec Create(string filePath, u8[] rawInput)
    {
        return new UnknownCodec(filePath, rawInput);
    }

    public override CodecId DefaultCheatOutputCodec => CodecId.UnsupportedCodecId;

    private UnknownCodec(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        return true;
    }

    public static bool Is(CodecId codecId)
    {
        return codecId == ThisCodecId;
    }

    private static AbstractBinaryScribe MakeScribe(u8[] rawInput)
    {
        return new BigEndianScribe(rawInput);
    }
}
