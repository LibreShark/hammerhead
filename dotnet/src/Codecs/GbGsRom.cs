using System.Text.RegularExpressions;
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

/// <summary>
/// GameShark and Action Replay for Game Boy and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbGsRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoy;
    private const CodecId ThisCodecId = CodecId.GbcGamesharkRom;

    private readonly List<RomString> _cheatNames = new();

    public GbGsRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        ReadVersion();
        ReadGames();
    }

    private void ReadVersion()
    {
        s32 arAddr1 = Buffer.Find("ACTION REPLAY");
        s32 gsAddr1 = Buffer.Find("GameShark");
        s32 arAddr2 = Buffer.Find("Action Replay");
        s32 thanksAddr = Buffer.Find("Thanks for ");
        s32 datelAddr = Buffer.Find("Datel Design");
        s32 verAddr = Buffer.Find("Ver. ");
        s32 proAddrOffset = Buffer[datelAddr..(datelAddr + 0xFF)].Find("PRO");
        s32 arAddr3Offset = Buffer[datelAddr..(datelAddr + 0xFF)].Find("ACTION REPLAY");
        s32 damonAddr = Buffer.Find("Damon Barwin");

        RomString arCaps1 = Scribe.Seek(arAddr1).ReadPrintableCString();
        RomString gsTitle = Scribe.Seek(gsAddr1).ReadPrintableCString();
        RomString arTitle = Scribe.Seek(arAddr2).ReadPrintableCString();
        RomString thanksStr = Scribe.Seek(thanksAddr).ReadPrintableCString();
        RomString datelDesign = Scribe.Seek(datelAddr).ReadPrintableCString();
        RomString versionStr = Scribe.Seek(verAddr).ReadPrintableCString();

        Metadata.Identifiers.Add(arCaps1);
        Metadata.Identifiers.Add(gsTitle);
        Metadata.Identifiers.Add(arTitle);
        Metadata.Identifiers.Add(thanksStr);
        Metadata.Identifiers.Add(datelDesign);
        Metadata.Identifiers.Add(versionStr);

        if (proAddrOffset != -1)
        {
            RomString pro = Scribe.Seek(datelAddr + proAddrOffset).ReadPrintableCString();
            Metadata.Identifiers.Add(pro);
        }

        if (arAddr3Offset != -1)
        {
            RomString arCaps2 = Scribe.Seek(datelAddr + arAddr3Offset).ReadPrintableCString();
            Metadata.Identifiers.Add(arCaps2);
        }

        if (damonAddr != -1)
        {
            RomString damonBarwin1 = Scribe.Seek(damonAddr).ReadPrintableCString();
            Metadata.Identifiers.Add(damonBarwin1);
        }

        s32 copySearchStart = datelAddr + datelDesign.Value.Length;
        s32 copySearchEnd = copySearchStart + 0xFF;
        s32 copyAddrOffset = Buffer[copySearchStart..copySearchEnd].Find("Copy");
        s32 copyAddr = copySearchStart + copyAddrOffset;
        Scribe.Seek(copyAddr);
        while (!Scribe.IsIntegerDigit())
        {
            Scribe.Next();
        }

        RomString yearStr = Scribe.ReadPrintableCString();
        Metadata.Identifiers.Add(yearStr);
        Metadata.BuildDateRaw = yearStr;

        // "Ver. 1.04"
        Match match = Regex.Match(versionStr.Value, @"[0-9]+\.[0-9]+");
        if (match.Success)
        {
            Metadata.DisplayVersion = $"v{match.Value}";
            Metadata.SortableVersion = Double.Parse(match.Value);
        }
    }

    private void ReadGames()
    {
        Scribe.Seek(0xA000);
        while (!Scribe.IsPadding())
        {
            RomString cheatName = Scribe.ReadCStringUntilNull(16, false).Trim();
            _cheatNames.Add(cheatName);
        }

        Scribe.Seek(0x10000);
        u32 gameIdx = 0;
        while (!Scribe.IsPadding())
        {
            RomString gameName = Scribe.ReadPrintableCString(16, false);
            u8 cheatCount = Scribe.ReadU8();
            var game = new Game()
            {
                GameIndex = gameIdx,
                GameName = gameName,
            };
            Games.Add(game);

            for (u32 cheatIdx = 0; cheatIdx < cheatCount; cheatIdx++)
            {
                u8[] codeBytes = Scribe.ReadBytes(4);
                Scribe.Skip(1);
                u8 cheatNameIndex = Scribe.ReadU8();
                var cheat = new Cheat()
                {
                    CheatIndex = cheatIdx,
                    CheatName = _cheatNames[cheatNameIndex],
                };
                game.Cheats.Add(cheat);

                // Each cheat has exactly 1 code
                var code = new Code()
                {
                    CodeIndex = 0,
                    Bytes = ByteString.CopyFrom(codeBytes),
                };
                cheat.Codes.Add(code);
            }
        }
    }

    public static bool Is(u8[] bytes)
    {
        bool is128KiB = bytes.IsKiB(128);
        return is128KiB && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        bool hasArUpperAddr = bytes.Contains("ACTION REPLAY");
        bool hasArTitleAddr = bytes.Contains("Action Replay");
        bool hasGsAddr = bytes.Contains("GameShark");
        bool hasThanksAddr = bytes.Contains("Thanks for ");
        bool hasDatelAddr = bytes.Contains("Datel Design");
        bool hasVerAddr = bytes.Contains("Ver. ");
        return hasArUpperAddr &&
               hasArTitleAddr &&
               hasGsAddr &&
               hasThanksAddr &&
               hasDatelAddr &&
               hasVerAddr;
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

    public override void PrintCustomHeader(TerminalPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading("Cheat names");
        for (int i = 0; i < _cheatNames.Count; i++)
        {
            RomString cheatName = _cheatNames[i];
            Console.WriteLine($"[{i:D2}]: {cheatName.Value}");
        }
    }
}
