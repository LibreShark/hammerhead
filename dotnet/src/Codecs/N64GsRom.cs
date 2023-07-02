using Google.Protobuf;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.N64;
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

/// <summary>
/// GameShark (USA/CAN), Action Replay (UK/EU), Equalizer (UK/EU), and Game Buster (Germany) for
/// Nintendo 64, made by Datel/InterAct.
/// </summary>
public sealed class N64GsRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.Nintendo64;
    private const CodecId ThisCodecId = CodecId.N64GamesharkRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, ThisCodecId, Create);

    public static N64GsRom Create(string filePath, u8[] rawInput)
    {
        return new N64GsRom(filePath, rawInput);
    }

    private readonly bool _isV3Firmware;
    private readonly bool _isV1GameList;
    private readonly bool _isV3KeyCodeListAddr;

    private readonly u32 _firmwareAddr;
    private readonly u32 _gameListAddr;
    private readonly u32 _keyCodeListAddr;
    private readonly u32 _userPrefsAddr;
    private readonly RomString _headerId;
    private readonly RomString _rawTimestamp;
    private readonly N64GsVersion _version;
    private readonly N64KeyCode _activeKeyCode;
    private readonly List<N64KeyCode> _keyCodes;

    private const u32 ProgramCounterAddr = 0x00000008;
    private const u32 ActiveKeyCodeAddr  = 0x00000010;
    private const u32 HeaderIdAddr       = 0x00000020;
    private const u32 BuildTimestampAddr = 0x00000030;

    public override CodecId DefaultCheatOutputCodec => CodecId.N64GamesharkText;

    private N64GsRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, Decrypt(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsKeyCodes = true;
        Support.SupportsFirmware = true;
        Support.SupportsFileEncryption = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;

        // TODO(CheatoBaggins): Decompress v3.x ROM files

        _headerId = Scribe.Seek(HeaderIdAddr).ReadCStringUntilNull(0x10, false);
        _rawTimestamp = Scribe.Seek(BuildTimestampAddr).ReadPrintableCString(15, true);

        Metadata.Identifiers.Add(_headerId);
        Metadata.Identifiers.Add(_rawTimestamp);

        _version = ReadVersion();

        Support.SupportsFirmwareCompression = _version.Number >= 2.5;

        Metadata.BrandId = _version.Brand;
        Metadata.BuildDateRaw = _rawTimestamp;
        Metadata.BuildDateIso = _version.DisplayBuildTimestampIso;
        Metadata.DisplayVersion = _version.DisplayNumber;
        Metadata.SortableVersion = _version.Number;
        Metadata.IsKnownVersion = _version.IsKnown;
        Metadata.LanguageIetfCode = _version.Locale.Name;

        _isV3Firmware        = Scribe.Seek(0x00001000).ReadU32() == 0x00000000;
        _isV1GameList        = Scribe.Seek(0x0002DFF0).ReadU32() == 0x00000000;
        _isV3KeyCodeListAddr = Scribe.Seek(0x0002FBF0).ReadU32() == 0xFFFFFFFF;
        _keyCodeListAddr     = (u32)(_isV3KeyCodeListAddr ? 0x0002FC00 : 0x0002D800);

        Support.SupportsUserPrefs = Scribe.Seek(0x0002FAF0).ReadU32() == 0xFFFFFFFF;
        Support.HasKeyCodes       = Scribe.Seek(_keyCodeListAddr).ReadU32() != 0x00000000;

        _firmwareAddr  = (u32)(_isV3Firmware ? 0x00001080 : 0x00001000);
        _gameListAddr  = (u32)(_isV1GameList ? 0x0002E000 : 0x00030000);
        _userPrefsAddr = Support.SupportsUserPrefs ? 0x0002FB00 : 0xFFFFFFFF;

        List<N64KeyCode> keyCodes = ReadKeyCodes();
        _activeKeyCode = ReadActiveKeyCode(keyCodes);
        _keyCodes = keyCodes;

        Support.IsFileEncrypted      = DetectEncrypted(rawInput);
        Support.IsFirmwareCompressed = DetectCompressed(rawInput);
        Support.HasPristineUserPrefs = Support.SupportsUserPrefs &&
                                       Scribe.Seek(_userPrefsAddr).IsPadding();

        Games.AddRange(ReadGames());
    }

    protected override ParsedFile ToProtoImpl()
    {
        var proto = base.ToProto();
        proto.KeyCodes.AddRange(_keyCodes.Select(kc => new Code()
        {
            CodeIndex = (u32)proto.KeyCodes.Count,
            Bytes = kc.Bytes,
            Comment = kc.Name.Value,
        }));
        return proto;
    }

    private List<Game> ReadGames()
    {
        Scribe.Seek(_gameListAddr);
        List<Game> games = new List<Game>();
        u32 gamesCount = Scribe.ReadU32();
        for (u32 gameIdx = 0; gameIdx < gamesCount; gameIdx++)
        {
            games.Add(ReadGame(gameIdx));
        }
        return games;
    }

    private Game ReadGame(u32 gameIdx)
    {
        var game = new Game()
        {
            GameIndex = gameIdx,
            GameName = ReadName(),
        };
        u8 cheatCount = Scribe.ReadU8();
        for (u8 cheatIdx = 0; cheatIdx < cheatCount; cheatIdx++)
        {
            ReadCheat(game, cheatIdx);
        }
        return game;
    }

    private void ReadCheat(Game game, u8 cheatIdx)
    {
        var cheat = new Cheat()
        {
            CheatIndex = cheatIdx,
            CheatName = ReadName(),
        };
        game.Cheats.Add(cheat);
        u8 codeCount = Scribe.ReadU8();
        bool cheatOn = (codeCount & 0x80) > 0;
        codeCount &= 0x7F;
        cheat.IsCheatActive = cheatOn;
        for (u8 codeIdx = 0; codeIdx < codeCount; codeIdx++)
        {
            ReadCode(cheat, codeIdx);
        }
    }

    private void ReadCode(Cheat cheat, u8 codeIdx)
    {
        u8[] bytes = Scribe.ReadBytes(6);
        cheat.Codes.Add(new Code()
        {
            CodeIndex = codeIdx,
            Bytes = ByteString.CopyFrom(bytes),
        });
    }

    private RomString ReadName()
    {
        u32 startPos = Scribe.Position;

        // Firmware and official Datel GS Utils do not support names longer than 30 chars.
        RomString name = Scribe.ReadPrintableCString();

        if (name.Value.Length < 1)
        {
            Console.Error.WriteLine($"WARNING at offset 0x{startPos:X8}: Game and Cheat names should contain at least 1 character.");
            return name;
        }

        name.Value = name.Value.Replace("`F6`", "Key");
        name.Value = name.Value.Replace("`F7`", "Have ");
        name.Value = name.Value.Replace("`F8`", "Lives");
        name.Value = name.Value.Replace("`F9`", "Energy");
        name.Value = name.Value.Replace("`FA`", "Health");
        name.Value = name.Value.Replace("`FB`", "Activate ");
        name.Value = name.Value.Replace("`FC`", "Unlimited ");
        name.Value = name.Value.Replace("`FD`", "Player ");
        name.Value = name.Value.Replace("`FE`", "Always ");
        name.Value = name.Value.Replace("`FF`", "Infinite ");

        return name;
    }

    private N64KeyCode ReadActiveKeyCode(List<N64KeyCode> keyCodes)
    {
        u8[] activeCrcBytes = Scribe.PeekBytesAt(ActiveKeyCodeAddr, 8);
        u8[] activePcBytes = Scribe.PeekBytesAt(ProgramCounterAddr, 4);

        RomString? name = null;
        if (keyCodes.Count > 0)
        {
            N64KeyCode? activeKeyCode = keyCodes.Find(kc =>
            {
                u8[] curKeyCodeCrcBytes = kc.Bytes.ToArray()[..8];
                return curKeyCodeCrcBytes.SequenceEqual(activeCrcBytes);
            });
            name = activeKeyCode?.Name;
        }

        return new N64KeyCode()
        {
            Name = name ?? new RomString() { Value = "probably CIC-NUS-6102" },
            Bytes = ByteString.CopyFrom(activeCrcBytes.Concat(activePcBytes).ToArray()),
            IsKeyCodeActive = true,
        };
    }

    private List<N64KeyCode> ReadKeyCodes()
    {
        u8[] activePrefix = Scribe.PeekBytesAt(ActiveKeyCodeAddr, 8);

        Scribe.Seek(_keyCodeListAddr);
        u8[] listBytes = Scribe.PeekBytes(0xA0);
        u32 maxPos = Scribe.Position + (u32)listBytes.Length;
        s32 keyCodeByteLength = listBytes.Find("Mario World 64 & Others");

        // Valid key codes are either 9 or 13 bytes long.
        if (keyCodeByteLength < 9)
        {
            return new List<N64KeyCode>();
        }

        List<N64KeyCode> keyCodes = new();
        while (Scribe.Position <= maxPos)
        {
            u8[] bytes = Scribe.ReadBytes((u32)keyCodeByteLength);
            RomString name = Scribe.ReadPrintableCString(0x1F, true);
            while (Scribe.PeekBytes(1)[0] == 0)
            {
                Scribe.ReadU8();
            }
            bool isActive = bytes.Contains(activePrefix);
            var keyCode = new N64KeyCode() {
                Name = name,
                Bytes = ByteString.CopyFrom(bytes),
                IsKeyCodeActive = isActive,
            };
            keyCodes.Add(keyCode);
        }
        return keyCodes;
    }

    private N64GsVersion ReadVersion()
    {
        // TODO(CheatoBaggins): Decompress v2.5+ firmware before scanning
        RomString? titleVersionNumberStr = ReadTitleVersion("N64 GameShark Version ") ??
                                           ReadTitleVersion("GameShark Pro Version ") ??
                                           ReadTitleVersion("N64 Action Replay Version ") ??
                                           ReadTitleVersion("Action Replay Pro Version ") ??
                                           ReadTitleVersion("N64 Equalizer Version ") ??
                                           ReadTitleVersion("N64 Game Buster Version ");

        if (titleVersionNumberStr != null)
        {
            Metadata.Identifiers.Add(titleVersionNumberStr);
        }

        N64GsVersion? version = N64GsVersion.From(_rawTimestamp.Value)?
            .WithTitleVersionNumber(titleVersionNumberStr?.Value);
        if (version == null)
        {
            throw new InvalidDataException("Failed to find N64 GameShark ROM version!");
        }

        return version;
    }

    private RomString? ReadTitleVersion(string needle)
    {
        u8[] haystack = Buffer[..0x30000];
        s32 titleVersionPos = haystack.Find(needle);
        bool isCompressed = titleVersionPos == -1;
        if (isCompressed)
        {
            return null;
        }

        // Uncomment to return ONLY the number (e.g., "2.21")
        // titleVersionPos += needle.Length;

        return Scribe.Seek((u32)titleVersionPos).ReadPrintableCString((u32)needle.Length + 5, true).Trim();
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        Scribe.Seek(_gameListAddr);
        Scribe.WriteU32((u32)Games.Count);
        foreach (Game game in Games)
        {
            Scribe.WriteCString(game.GameName.Value, 30);
            Scribe.WriteU8((u8)game.Cheats.Count);
            foreach (Cheat cheat in game.Cheats)
            {
                Scribe.WriteCString(cheat.CheatName.Value, 30);
                u8 codeCount = (u8)cheat.Codes.Count;
                if (cheat.IsCheatActive)
                {
                    codeCount |= 0x80;
                }
                Scribe.WriteU8(codeCount);
                foreach (Code code in cheat.Codes)
                {
                    Scribe.WriteBytes(code.Bytes);
                }
            }
        }

        u8 pad = Buffer.Last();
        while (!Scribe.EndReached)
        {
            Scribe.WriteU8(pad);
        }

        return this;
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        return is256KiB && (DetectPlain(bytes) || DetectEncrypted(bytes));
    }

    public static bool Is(AbstractCodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }

    private static bool DetectPlain(u8[] bytes)
    {
        u8[] first4Bytes = bytes[..4];
        bool isN64 = first4Bytes.SequenceEqual(new u8[] { 0x80, 0x37, 0x12, 0x40 }) ||
                     first4Bytes.SequenceEqual(new u8[] { 0x80, 0x37, 0x12, 0x00 });
        const string v1Or2Header = "(C) DATEL D&D ";
        const string v3ProHeader = "(C) MUSHROOM &";
        return isN64 && (bytes.Contains(v1Or2Header) || bytes.Contains(v3ProHeader));
    }

    private static bool DetectEncrypted(u8[] bytes)
    {
        return bytes[..7].Contains(new u8[] { 0xAE, 0x59, 0x63, 0x54 });
    }

    private static bool DetectCompressed(u8[] bytes)
    {
        // The main menu title is stored in the firmware section of the ROM,
        // so the title will not be found in plain text in compressed files.
        return !bytes.Contains(" Version ");
    }

    public override u8[] Encrypt()
    {
        return N64GsCrypter.Encrypt(Buffer);
    }

    private static AbstractBinaryScribe Decrypt(u8[] input)
    {
        u8[] output = DetectEncrypted(input)
            ? N64GsCrypter.Decrypt(input)
            : input.ToArray();
        return new BigEndianScribe(output);
    }

    public override void PrintCustomHeader(TerminalPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading("Addresses");
        PrintAddressTable(printer);

        printer.PrintHeading("Key codes");
        Console.WriteLine($"Active key code: {_activeKeyCode.ToDisplayString()}");
        Console.WriteLine();

        if (Support.SupportsKeyCodes && Support.HasKeyCodes)
        {
            PrintKeyCodesTable(printer);
        }
        else
        {
            printer.PrintHint("This firmware version does not support additional key codes.");
        }
    }

    private void PrintAddressTable(TerminalPrinter printer)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Section"))
                .AddColumn(printer.HeaderCell("Address"))
            ;

        string firmwareAddr = $"0x{_firmwareAddr:X8}";
        string gameListAddr = $"0x{_gameListAddr:X8}";
        string keyCodeListAddr = Support.SupportsKeyCodes ? $"0x{_keyCodeListAddr:X8}" : "Not supported";
        string userPrefsAddr = Support.SupportsUserPrefs ? $"0x{_userPrefsAddr:X8}" : "Not supported";

        table.AddRow("Firmware", firmwareAddr);
        table.AddRow("User prefs", userPrefsAddr);
        table.AddRow("Key code list", keyCodeListAddr);
        table.AddRow("Game list", gameListAddr);

        printer.PrintTable(table);
    }

    private void PrintKeyCodesTable(TerminalPrinter printer)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Games (CIC chip)"))
                .AddColumn(printer.HeaderCell("Key code"))
                .AddColumn(printer.HeaderCell("Active?"))
            ;

        foreach (N64KeyCode keyCode in _keyCodes)
        {
            string keyCodeName = keyCode.Name.Value.EscapeMarkup();
            string hexString = keyCode.Bytes.ToHexString(" ");
            table.AddRow(
                keyCode.IsKeyCodeActive
                    ? $"> {keyCodeName} [ACTIVE]".EscapeMarkup()
                    : $"  {keyCodeName}",
                keyCode.IsKeyCodeActive
                    ? hexString
                    : ""
            );
        }

        printer.PrintTable(table);
    }
}
