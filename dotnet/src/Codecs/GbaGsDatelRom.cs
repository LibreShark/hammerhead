using Google.Protobuf;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Codecs;

// ReSharper disable BuiltInTypeReferenceStyle
using u8 = Byte;
using u16 = UInt16;
using u32 = UInt32;

/// <summary>
/// GameShark and Action Replay for Game Boy Advance,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbaGsDatelRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyAdvance;
    private const CodecId ThisCodecId = CodecId.GbaGamesharkDatelRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, ThisCodecId, Create);

    public static GbaGsDatelRom Create(string filePath, u8[] rawInput)
    {
        return new GbaGsDatelRom(filePath, rawInput);
    }

    private const u32 GbaMagicStrAddr = 0x21000;
    private const u32 MinorVersionNumberAddr = 0x21004;
    private const u32 MajorVersionNumberAddr = 0x21005;
    private const u32 GameListAddr = 0x21040;

    private static readonly string[] KnownVersions =
    {
        "v1.0",
        "v3.3",
        "v5.8",
    };

    private readonly AbstractBinaryScribe _beScribe = new BigEndianScribe(new byte[8]);

    public override CodecId DefaultCheatOutputCodec => CodecId.UnsupportedCodecId;

    private GbaGsDatelRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsFirmware = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;

        // TODO(CheatoBaggins): Detect
        Support.SupportsUserPrefs = true;
        Support.HasPristineUserPrefs = false;

        ParseVersion();
        ParseGames();
    }

    private void ParseVersion()
    {
        u8 minorVersionNumber = Buffer[MinorVersionNumberAddr];
        u8 majorVersionNumber = Buffer[MajorVersionNumberAddr];
        Metadata.DisplayVersion = $"v{majorVersionNumber}.{minorVersionNumber}";
        Metadata.SortableVersion = Double.Parse($"{majorVersionNumber}.{minorVersionNumber}");
        Metadata.IsKnownVersion = KnownVersions.Contains(Metadata.DisplayVersion);

        RomString versionIdStr =
            Scribe
                .Seek(GbaMagicStrAddr)
                .ReadCStringUntilNull(4, false);
        versionIdStr.Value += $"{majorVersionNumber}{minorVersionNumber}";
        versionIdStr.Addr.EndIndex += 2;
        versionIdStr.Addr.Length += 2;
        Metadata.Identifiers.Add(versionIdStr);
    }

    private void ParseGames()
    {
        Scribe.Seek(GameListAddr);

        u16 gameCount = Scribe.ReadU16();
        Scribe.Skip(2); // null byte padding

        for (u16 gameIdx = 0; gameIdx < gameCount; gameIdx++)
        {
            RomString gameName = Scribe.ReadPrintableCString(20, false).Trim();
            var game = new Game()
            {
                GameIndex = gameIdx,
                GameName = gameName,
            };
            Games.Add(game);

            u8 cheatCount = Scribe.ReadU8();
            Scribe.Skip(3); // null byte padding

            for (u8 cheatIdx = 0; cheatIdx < cheatCount; cheatIdx++)
            {
                u8 codeCount = (u8)(Scribe.ReadU8() / 2);
                Scribe.Skip(2); // null byte padding
                u8 unknownByte2 = Scribe.ReadU8();

                RomString cheatName = Scribe.ReadPrintableCString(20, false).Trim();
                var cheat = new Cheat()
                {
                    CheatIndex = cheatIdx,
                    CheatName = cheatName,
                };
                game.Cheats.Add(cheat);

                for (u8 codeIdx = 0; codeIdx < codeCount; codeIdx++)
                {
                    // Codes are stored in the ROM in little-endian order,
                    // but displayed in the UI (and entered by the user) in
                    // big-endian order.
                    u32 addr = Scribe.ReadU32();
                    u32 value = Scribe.ReadU32();
                    _beScribe.Seek(0).WriteU32(addr).WriteU32(value);
                    var code = new Code()
                    {
                        CodeIndex = codeIdx,
                        Bytes = ByteString.CopyFrom(_beScribe.GetBufferCopy()),
                    };
                    cheat.Codes.Add(code);
                }
            }
        }
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        bool is512KiB = bytes.IsKiB(512);
        return (is256KiB || is512KiB) && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        bool hasMagicNumber = bytes[..4].SequenceEqual(new u8[] { 0x2E, 0x00, 0x00, 0xEA });
        bool hasMagicText = bytes[0x21000..0x21004].ToAsciiString().Equals("GBA_");
        return hasMagicNumber && hasMagicText;
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
        return new LittleEndianScribe(rawInput);
    }
}
