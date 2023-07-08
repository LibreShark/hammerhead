using System.Collections.Immutable;
using Google.Protobuf.Collections;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Cli;
using Spectre.Console;

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

public interface ICodec
{
    ImmutableArray<u8> RawInput { get; }

    /// <summary>
    /// Plain, unencrypted, unobfuscated copy of the internal ROM bytes.
    /// If the input file is encrypted/scrambled, it must be
    /// decrypted/unscrambled immediately in the subclass constructor.
    /// </summary>
    u8[] Buffer { get; }

    FileMetadata Metadata { get; }
    RepeatedField<Game> Games { get; }
    CodecFeatureSupport Support { get; }
    CodecId DefaultCheatOutputCodec { get; }
    ICodec ImportFromProto(ParsedFile parsed);
    ParsedFile ToFullProto();
    ParsedFile ToSlimProto();
    void PrintCustomHeader(ICliPrinter printer, InfoCmdParams @params);
    void PrintGames(ICliPrinter printer, InfoCmdParams @params);
    void PrintCustomBody(ICliPrinter printer, InfoCmdParams @params);
    void AddFileProps(Table table);
    bool SupportsCheats();
    bool SupportsFileSplitting();
    bool SupportsFileEncryption();
    bool SupportsFileScrambling();
    bool SupportsFirmwareCompression();
    bool SupportsUserPrefs();
    bool IsFileEncrypted();
    bool IsFileScrambled();
    bool IsFirmwareCompressed();
    bool HasPristineUserPrefs();
    u8[] Encrypt();
    u8[] Decrypt();
    u8[] Scramble();
    u8[] Unscramble();
    // TODO(CheatoBaggins): What should this return?
    u8[] Split();
    ICodec WriteChangesToBuffer();
    bool IsValidFormat();
    bool IsInvalidFormat();
}
