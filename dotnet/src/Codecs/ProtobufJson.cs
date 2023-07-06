using Google.Protobuf;
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

public sealed class ProtobufJson : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.Universal;
    private const CodecId ThisCodecId = CodecId.HammerheadJson;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static ProtobufJson Create(string filePath, u8[] rawInput)
    {
        var codec = new ProtobufJson(filePath, rawInput);
        if (rawInput.Length > 0)
        {
            HammerheadDump dump = HammerheadDump.Parser.ParseJson(rawInput.ToUtf8String());
            codec.ImportFromProto(dump.ParsedFiles.First());
        }
        return codec;
    }

    public override CodecId DefaultCheatOutputCodec => CodecId.HammerheadJson;

    private ProtobufJson(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsUserPrefs = true;
        Support.SupportsKeyCodes = true;
        Support.SupportsSmxMessages = true;
    }

    public override ICodec WriteChangesToBuffer()
    {
        var formatter = new JsonFormatter(
            JsonFormatter.Settings.Default
                .WithIndentation()
                .WithFormatDefaultValues(false)
                .WithPreserveProtoFieldNames(true)
        );
        Buffer = $"{formatter.Format(Dump)}\n".ToUtf8Bytes();
        return this;
    }

    public static bool Is(u8[] bytes)
    {
        try
        {
            HammerheadDump? parsed = HammerheadDump.Parser.ParseJson(bytes.ToUtf8String());
            return parsed != null;
        }
        catch
        {
            return false;
        }
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
