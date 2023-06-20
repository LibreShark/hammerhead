using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.N64;

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
/// GameShark (USA/CAN), Action Replay (UK/EU), Equalizer (UK/EU), and Game Buster (Germany) for
/// Nintendo 64, made by Datel/InterAct.
/// </summary>
public sealed class N64GsRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.N64Gameshark;

    private readonly BigEndianScribe _scribe;

    private bool _isCompressed;
    private readonly bool _isV3Firmware;
    private readonly bool _isV1GameList;
    private readonly bool _isV3KeyCodeListAddr;
    private readonly bool _supportsUserPrefs;
    private readonly bool _supportsKeyCodes;

    private readonly u32 _firmwareAddr;
    private readonly u32 _gameListAddr;
    private readonly u32 _keyCodeListAddr;
    private readonly u32 _userPrefsAddr;
    private readonly RomString _headerId;
    private readonly RomString _rawTimestamp;
    private readonly N64GsVersion _version;
    private readonly KeyCode _activeKeyCode;
    private readonly List<KeyCode> _keyCodes;

    private const u32 ProgramCounterAddr = 0x00000008;
    private const u32 ActiveKeyCodeAddr  = 0x00000010;
    private const u32 HeaderIdAddr       = 0x00000020;
    private const u32 BuildTimestampAddr = 0x00000030;

    public N64GsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
        if (IsFileEncrypted())
        {
            Decrypt();
        }

        // TODO(CheatoBaggins): Decompress v3.x ROM files

        _scribe = new BigEndianScribe(Bytes);

        _headerId = _scribe.Seek(HeaderIdAddr).ReadCStringUntilNull(0x10);
        _rawTimestamp = _scribe.Seek(BuildTimestampAddr).ReadPrintableCString(15);

        Metadata.Identifiers.Add(_headerId);
        Metadata.Identifiers.Add(_rawTimestamp);

        _version = ReadVersion();

        Metadata.Brand = _version.Brand;
        Metadata.BuildDateRaw = _rawTimestamp;
        Metadata.BuildDateIso = _version.DisplayBuildTimestampIso;
        Metadata.DisplayVersion = _version.DisplayNumber;
        Metadata.SortableVersion = _version.Number;
        Metadata.IsKnownVersion = _version.IsKnown;
        Metadata.LanguageIetfCode = _version.Locale.Name;

        _isV3Firmware        = _scribe.Seek(0x00001000).ReadU32() == 0x00000000;
        _isV1GameList        = _scribe.Seek(0x0002DFF0).ReadU32() == 0x00000000;
        _isV3KeyCodeListAddr = _scribe.Seek(0x0002FBF0).ReadU32() == 0xFFFFFFFF;
        _keyCodeListAddr     = (u32)(_isV3KeyCodeListAddr ? 0x0002FC00 : 0x0002D800);
        _supportsKeyCodes    = _scribe.Seek(_keyCodeListAddr).ReadU32() != 0x00000000;
        _supportsUserPrefs   = _scribe.Seek(0x0002FAF0).ReadU32() == 0xFFFFFFFF;
        _firmwareAddr        = (u32)(_isV3Firmware ? 0x00001080 : 0x00001000);
        _gameListAddr        = (u32)(_isV1GameList ? 0x0002E000 : 0x00030000);
        _userPrefsAddr       = _supportsUserPrefs ? 0x0002FB00 : 0xFFFFFFFF;

        List<KeyCode> keyCodes = ReadKeyCodes();
        _activeKeyCode = ReadActiveKeyCode(keyCodes);
        _keyCodes = keyCodes;

        Games = ReadGames();
    }

    public override bool FormatSupportsFileEncryption()
    {
        return true;
    }

    public override bool FormatSupportsFirmwareCompression()
    {
        return true;
    }

    public override bool FormatSupportsUserPrefs()
    {
        return _supportsUserPrefs;
    }

    public override bool HasUserPrefs()
    {
        if (!_supportsUserPrefs)
        {
            return false;
        }
        return _scribe.MaintainPosition(() => !_scribe.Seek(_userPrefsAddr).IsPadding());
    }

    private List<Game> ReadGames()
    {
        List<Game> games = new List<Game>();
        _scribe.Seek(_gameListAddr);
        u32 gamesCount = _scribe.ReadU32();
        for (u32 gameIdx = 0; gameIdx < gamesCount; gameIdx++)
        {
            games.Add(ReadGame());
        }
        return games;
    }

    private Game ReadGame()
    {
        Game game = Game.NewGame(ReadName());
        u8 cheatCount = _scribe.ReadU8();
        for (u8 cheatIdx = 0; cheatIdx < cheatCount; cheatIdx++)
        {
            ReadCheat(game);
        }
        return game;
    }

    private void ReadCheat(Game game)
    {
        Cheat cheat = game.AddCheat(ReadName());
        u8 codeCount = _scribe.ReadU8();
        bool cheatOn = (codeCount & 0x80) > 0;
        codeCount &= 0x7F;
        cheat.IsActive = cheatOn;
        for (u8 codeIdx = 0; codeIdx < codeCount; codeIdx++)
        {
            ReadCode(cheat);
        }
    }

    private void ReadCode(Cheat cheat)
    {
        byte[] address = _scribe.ReadBytes(4);
        byte[] value = _scribe.ReadBytes(2);
        cheat.AddCode(address, value);
    }

    private string ReadName()
    {
        u32 pos = _scribe.Position;

        // Firmware does not support names longer than 30 chars.
        string name = _scribe.ReadCStringUntilNull(30).Value;

        if (name.Length < 1)
        {
            Console.Error.WriteLine($"WARNING at offset 0x{pos:X8}: Game and Cheat names should contain at least 1 character.");
            return name;
        }

        name = name.Replace("`F6`", "Key");
        name = name.Replace("`F7`", "Have ");
        name = name.Replace("`F8`", "Lives");
        name = name.Replace("`F9`", "Energy");
        name = name.Replace("`FA`", "Health");
        name = name.Replace("`FB`", "Activate ");
        name = name.Replace("`FC`", "Unlimited ");
        name = name.Replace("`FD`", "Player ");
        name = name.Replace("`FE`", "Always ");
        name = name.Replace("`FF`", "Infinite ");

        return name;
    }

    private KeyCode ReadActiveKeyCode(List<KeyCode> keyCodes)
    {
        byte[] crcBytes = _scribe.PeekBytesAt(ActiveKeyCodeAddr, 8);
        byte[] pcBytes = _scribe.PeekBytesAt(ProgramCounterAddr, 4);

        string? name = null;
        if (keyCodes.Count > 0)
        {
            KeyCode? activeKeyCode = keyCodes.Find(kc => kc.ChecksumBytes.SequenceEqual(crcBytes));
            name = activeKeyCode?.Name;
        }

        return new KeyCode(name ?? "probably CIC-NUS-6102", crcBytes.Concat(pcBytes).ToArray(), true);
    }

    private List<KeyCode> ReadKeyCodes()
    {
        byte[] activePrefix = _scribe.PeekBytesAt(ActiveKeyCodeAddr, 8);

        _scribe.Seek(_keyCodeListAddr);
        byte[] listBytes = _scribe.PeekBytes(0xA0);
        u32 maxPos = _scribe.Position + (u32)listBytes.Length;
        s32 keyCodeByteLength = listBytes.Find("Mario World 64 & Others");

        // Valid key codes are either 9 or 13 bytes long.
        if (keyCodeByteLength < 9)
        {
            return new List<KeyCode>();
        }

        List<KeyCode> keyCodes = new();
        while (_scribe.Position <= maxPos)
        {
            byte[] bytes = _scribe.ReadBytes((u32)keyCodeByteLength);
            RomString name = _scribe.ReadPrintableCString(0x1F);
            while (_scribe.PeekBytes(1)[0] == 0)
            {
                _scribe.ReadU8();
            }
            bool isActive = bytes.Contains(activePrefix);
            KeyCode keyCode = new(name.Value, bytes, isActive);
            keyCodes.Add(keyCode);
        }
        return keyCodes;
    }

    private N64GsVersion ReadVersion()
    {
        // TODO(CheatoBaggins): Decompress v2.5+ firmware before scanning
        RomString? titleVersionNumberStr = ReadTitleVersion("N64 GameShark Version ") ??
                                           ReadTitleVersion("N64 Action Replay Version ") ??
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
        byte[] haystack = Bytes[..0x30000];
        s32 titleVersionPos = haystack.Find(needle);
        _isCompressed = titleVersionPos == -1;
        if (_isCompressed)
        {
            return null;
        }

        // Uncomment to return ONLY the number (e.g., "2.21")
        // titleVersionPos += needle.Length;

        return _scribe.Seek((u32)titleVersionPos).ReadPrintableCString((u32)needle.Length + 5).Trim();
    }

    public override bool IsFileEncrypted()
    {
        return DetectEncrypted(InitialBytes.ToArray());
    }

    public override bool IsFirmwareCompressed()
    {
        return _isCompressed;
    }

    private void Decrypt()
    {
        if (!DetectEncrypted(Bytes))
        {
            return;
        }

        byte[] decrypted = N64GsCrypter.Decrypt(Bytes);
        for (u32 i = 0; i < Bytes.Length; i++)
        {
            Bytes[i] = decrypted[i];
        }
    }

    public byte[] GetEncrypted()
    {
        return N64GsCrypter.Encrypt(Bytes);
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        return is256KiB && (DetectPlain(bytes) || DetectEncrypted(bytes));
    }

    private static bool DetectPlain(byte[] bytes)
    {
        byte[] first4Bytes = bytes[..4];
        bool isN64 = first4Bytes.SequenceEqual(new byte[] { 0x80, 0x37, 0x12, 0x40 }) ||
                     first4Bytes.SequenceEqual(new byte[] { 0x80, 0x37, 0x12, 0x00 });
        const string v1Or2Header = "(C) DATEL D&D ";
        const string v3ProHeader = "(C) MUSHROOM &";
        return isN64 && (bytes.Contains(v1Or2Header) || bytes.Contains(v3ProHeader));
    }

    private static bool DetectEncrypted(byte[] bytes)
    {
        return bytes[..7].Contains(new byte[] { 0xAE, 0x59, 0x63, 0x54 });
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Format == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    protected override void PrintCustomHeader()
    {
        string firmwareAddr = $"0x{_firmwareAddr:X8}";
        string gameListAddr = $"0x{_gameListAddr:X8}";
        string keyCodeListAddr = _supportsKeyCodes ? $"0x{_keyCodeListAddr:X8}" : "Not supported";
        string userPrefsAddr = (_supportsUserPrefs ? $"0x{_userPrefsAddr:X8}" : "Not supported");

        Console.WriteLine($"Firmware addr:      {firmwareAddr}");
        Console.WriteLine($"User prefs addr:    {userPrefsAddr}");
        Console.WriteLine($"Key code list addr: {keyCodeListAddr}");
        Console.WriteLine($"Game list addr:     {gameListAddr}");
        Console.WriteLine();
        Console.WriteLine($"Active key code: {_activeKeyCode}");

        if (_supportsKeyCodes)
        {
            Console.WriteLine("Key codes: ");
            foreach (KeyCode keyCode in _keyCodes)
            {
                Console.WriteLine($"- {keyCode}");
            }
        }
        else
        {
            Console.WriteLine("This firmware version does not support additional key codes.");
        }
    }
}
