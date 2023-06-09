using Google.Protobuf;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.GameBoyColor;

/// <summary>
/// Xploder GB (aka "Xplorer GB") for Game Boy Color and Game Boy Pocket,
/// made by Blaze and Future Console Design (FCD).
/// </summary>
public sealed class GbcXpRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyColor;
    private const CodecId ThisCodecId = CodecId.GbcXploderRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static GbcXpRom Create(string filePath, u8[] rawInput)
    {
        return new GbcXpRom(filePath, rawInput);
    }

    private const u32 ProductIdAddr    = 0x00000000;
    private const u32 ManufacturerAddr = 0x00000104;
    private const u32 GameListAddr     = 0x00020000;

    public override CodecId DefaultCheatOutputCodec => CodecId.HammerheadJson;

    private GbcXpRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsFirmware = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;

        // TODO(CheatoBaggins): Detect
        Support.SupportsUserPrefs = true;
        Support.HasPristineUserPrefs = false;

        u32 productNameAddr = (u32)Scribe.Find("Xploder GB");
        Metadata.BrandId = BrandId.Xploder;

        RomString productId = Scribe.Seek(ProductIdAddr).ReadPrintableCString();
        RomString productName = Scribe.Seek(productNameAddr).ReadPrintableCString();
        RomString manufacturer = Scribe.Seek(ManufacturerAddr).ReadPrintableCString();

        Metadata.Identifiers.Add(productId);
        Metadata.Identifiers.Add(productName);
        Metadata.Identifiers.Add(manufacturer);

        Scribe.Seek(GameListAddr);
        ReadGames();
    }

    private void ReadGames()
    {
        u32 gameIdx = 0;
        while (true)
        {
            RomString gameName = Scribe.ReadPrintableCString();
            u8 cheatCount = Scribe.ReadU8();

            if (gameName.Value.Length == 0)
            {
                break;
            }

            var game = new Game()
            {
                GameIndex = gameIdx,
                GameName = gameName,
            };

            for (u8 cheatIdx = 0; cheatIdx < cheatCount; cheatIdx++)
            {
                RomString cheatName = Scribe.ReadPrintableCString();
                u8 codeCount = Scribe.ReadU8();

                var cheat = new Cheat()
                {
                    CheatIndex = cheatIdx,
                    CheatName = cheatName,
                };

                for (u8 codeIdx = 0; codeIdx < codeCount; codeIdx++)
                {
                    var code = new Code()
                    {
                        CodeIndex = codeIdx,
                        Bytes = ByteString.CopyFrom(Scribe.ReadBytes(4)),
                    };

                    cheat.Codes.Add(code);
                }

                game.Cheats.Add(cheat);
            }

            Games.Add(game);
            gameIdx++;
        }
    }

    public override ICodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        return is256KiB && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        return bytes[..10].ToAsciiString() == "Xplorer-GB" &&
               bytes.Contains("Future Console Design!");
    }

    public static bool Is(ICodec codec)
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
