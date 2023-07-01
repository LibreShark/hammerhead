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
/// GameShark and Action Replay for the original Game Boy and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GboGsRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyOriginal;
    private const CodecId ThisCodecId = CodecId.GboGamesharkRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, ThisCodecId, Create);

    public static GboGsRom Create(string filePath, u8[] rawInput)
    {
        return new GboGsRom(filePath, rawInput);
    }

    private readonly List<RomString> _cheatNames = new();

    public override CodecId DefaultCheatOutputCodec => CodecId.UnsupportedCodecId;

    private GboGsRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsFirmware = true;
        Support.SupportsUserPrefs = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;
        // TODO(CheatoBaggins): Detect
        Support.HasDirtyUserPrefs = false;

        ReadVersion();
        ReadGames();
    }

    private void ReadVersion()
    {
        s32 arUpperAddr = Buffer.Find("ACTION REPLAY");
        s32 arTitleAddr = Buffer.Find("Action Replay");
        s32 gsUpperAddr = Buffer.Find("GAMESHARK");
        s32 gsTitleAddr = Buffer.Find("GameShark");
        s32 thanksAddr = Buffer.Find("Thanks for ");
        s32 datelAddr = Buffer.Find("Datel Design");
        s32 verAddr = Buffer.Find("Ver. ");
        s32 proAddrOffset = Buffer[datelAddr..(datelAddr + 0xFF)].Find("PRO");
        s32 arUpperAddrOffset = Buffer[datelAddr..(datelAddr + 0xFF)].Find("ACTION REPLAY");
        s32 damonAddr = Buffer.Find("Damon Barwin");

        if (gsUpperAddr > -1 || gsTitleAddr > -1)
        {
            Metadata.BrandId = BrandId.Gameshark;
        }
        else if (arUpperAddr > -1 || arTitleAddr > -1)
        {
            Metadata.BrandId = BrandId.ActionReplay;
        }

        if (arUpperAddr != -1)
        {
            RomString arUpperStr = Scribe.Seek(arUpperAddr).ReadPrintableCString();
            Metadata.Identifiers.Add(arUpperStr);
        }
        if (arTitleAddr != -1)
        {
            RomString arTitleStr = Scribe.Seek(arTitleAddr).ReadPrintableCString();
            Metadata.Identifiers.Add(arTitleStr);
        }
        if (gsUpperAddr != -1)
        {
            RomString gsUpperStr = Scribe.Seek(gsUpperAddr).ReadPrintableCString();
            Metadata.Identifiers.Add(gsUpperStr);
        }
        if (gsTitleAddr != -1)
        {
            RomString gsTitleStr = Scribe.Seek(gsTitleAddr).ReadPrintableCString();
            Metadata.Identifiers.Add(gsTitleStr);
        }
        if (thanksAddr != -1)
        {
            RomString thanksStr = Scribe.Seek(thanksAddr).ReadPrintableCString();
            Metadata.Identifiers.Add(thanksStr);
        }

        RomString datelStr = Scribe.Seek(datelAddr).ReadPrintableCString();
        Metadata.Identifiers.Add(datelStr);

        RomString versionStr = Scribe.Seek(verAddr).ReadPrintableCString();
        Metadata.Identifiers.Add(versionStr);

        if (proAddrOffset != -1)
        {
            RomString pro = Scribe.Seek(datelAddr + proAddrOffset).ReadPrintableCString();
            Metadata.Identifiers.Add(pro);
        }

        if (arUpperAddrOffset != -1)
        {
            RomString arCaps2 = Scribe.Seek(datelAddr + arUpperAddrOffset).ReadPrintableCString();
            Metadata.Identifiers.Add(arCaps2);
        }

        if (damonAddr != -1)
        {
            RomString damonBarwin1 = Scribe.Seek(damonAddr).ReadPrintableCString();
            Metadata.Identifiers.Add(damonBarwin1);
        }

        s32 copyAddr = Buffer.Find("Copyright");
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

    public override AbstractCodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        bool is128KiB = bytes.IsKiB(128);
        return is128KiB && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        bool hasArUpper = bytes.Contains("ACTION REPLAY");
        bool hasArTitle = bytes.Contains("Action Replay");
        bool hasGsUpper = bytes.Contains("GAMESHARK");
        bool hasGsTitle = bytes.Contains("GameShark");
        // bool hasThanks = bytes.Contains("Thanks for ");
        bool hasDatel = bytes.Contains("Datel Design");
        bool hasVer = bytes.Contains("Ver. ");
        return (hasArUpper || hasArTitle) &&
               (hasGsUpper || hasGsTitle) &&
               hasDatel &&
               hasVer;
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
        printer.PrintHeading("Cheat name lookup table");
        Console.WriteLine();

        for (int i = 0; i < _cheatNames.Count; i++)
        {
            RomString cheatName = _cheatNames[i];
            Console.WriteLine($"[{i:D2}]: {cheatName.Value}");
        }

        Console.WriteLine();
    }
}
