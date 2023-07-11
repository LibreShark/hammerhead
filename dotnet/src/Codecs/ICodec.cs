using System.Collections.Immutable;
using Google.Protobuf.Collections;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Cli;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.Nintendo64;
using Spectre.Console;

namespace LibreShark.Hammerhead.Codecs;

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
    List<EmbeddedFile> EmbeddedFiles { get; }
    List<EmbeddedImage> EmbeddedImages { get; }
    ICodec ImportFromProto(ParsedFile parsed);
    ParsedFile ToFullProto();
    ParsedFile ToSlimProto();
    void PrintCustomHeader(ICliPrinter printer, InfoCmdParams @params);
    void PrintGames(ICliPrinter printer, InfoCmdParams @params);
    void PrintCustomBody(ICliPrinter printer, InfoCmdParams @params);
    void AddFileProps(Table table);
    bool SupportsCheats();
    bool SupportsFileExtraction();
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
    void ReorderKeyCodes(N64KeyCodeId[] newKeyCodeIds);
    ICodec WriteChangesToBuffer();
    bool IsValidFormat();
    bool IsInvalidFormat();
}
