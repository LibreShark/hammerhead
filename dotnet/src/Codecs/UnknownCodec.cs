using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Codecs;

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

    public override ICodec WriteChangesToBuffer()
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
