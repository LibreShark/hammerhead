using Google.Protobuf;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Roms;

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
/// Xploder GB (aka "Xplorer GB") for Game Boy Color and Game Boy Pocket,
/// made by Blaze and Future Console Design (FCD).
/// </summary>
public sealed class GbcXpRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.GameBoyColor;
    private const RomFormat ThisRomFormat = RomFormat.GbcXploder;

    private const u32 ProductIdAddr    = 0x00000000;
    private const u32 ManufacturerAddr = 0x00000104;
    private const u32 GameListAddr     = 0x00020000;

    public GbcXpRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
        u32 productNameAddr = (u32)Scribe.Find("Xploder GB");
        Metadata.Brand = RomBrand.Xploder;

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

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Format == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    private static BinaryScribe MakeScribe(u8[] rawInput)
    {
        return new LittleEndianScribe(rawInput.ToArray());
    }
}
