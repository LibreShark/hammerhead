using Google.Protobuf;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.GameBoyColor;

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
/// "Game Boy Code File" (*.gcf) binary format for the official PC utils for
/// v3.X GameShark and Action Replay for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcGsV3CodeFile : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyColor;
    private const CodecId ThisCodecId = CodecId.GbcGamesharkV3Rom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static GbcGsV3CodeFile Create(string filePath, u8[] rawInput)
    {
        return new GbcGsV3CodeFile(filePath, rawInput);
    }

    public override CodecId DefaultCheatOutputCodec => ThisCodecId;

    private GbcGsV3CodeFile(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;

        ReadGames();
    }

    private void ReadGames()
    {
        Scribe.Seek(0x18);
        u16 gameCount = Scribe.ReadU16();
        for (u16 i = 0; i < gameCount; i++)
        {
            // TODO(CheatoBaggins): Implement
            // See:
            // https://github.com/visualboyadvance-m/visualboyadvance-m/blob/24b92462f90ba44a218daa5a4cc5db1b27446119/src/wx/cmdevents.cpp#L430
            // https://github.com/visualboyadvance-m/visualboyadvance-m/blob/24b92462f90ba44a218daa5a4cc5db1b27446119/src/gb/gbCheats.cpp#L405
        }
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        // TODO(CheatoBaggins): Is there a way to detect these?
        return false;
    }

    public static bool Is(AbstractCodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }

    private static AbstractBinaryScribe MakeScribe(u8[] rawInput)
    {
        return new LittleEndianScribe(rawInput.ToArray());
    }
}
