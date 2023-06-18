using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.N64;

namespace LibreShark.Hammerhead.Roms;

/// <summary>
/// GameShark (USA/CAN), Action Replay (UK/EU), Equalizer (UK/EU), and Game Buster (Germany) for
/// Nintendo 64, made by Datel/InterAct.
/// </summary>
public sealed class N64GsRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.N64Gameshark;

    private readonly BigEndianReader _reader;
    private readonly BigEndianWriter _writer;

    private bool _isCompressed;
    private bool _isV3Firmware;
    private bool _isV1GameList;
    private bool _isV3KeyCodeListAddr;
    private bool _supportsUserPrefs;
    private bool _supportsKeyCodes;

    private uint _firmwareAddr;
    private uint _gameListAddr;
    private uint _keyCodeListAddr;
    private uint _userPrefsAddr;
    private RomString _headerId;
    private RomString _rawTimestamp;
    private N64GsVersion _version;
    private KeyCode _activeKeyCode;
    private List<KeyCode> _keyCodes;

    private const uint ProgramCounterAddr = 0x00000008;
    private const uint ActiveKeyCodeAddr  = 0x00000010;
    private const uint HeaderIdAddr       = 0x00000020;
    private const uint BuildTimestampAddr = 0x00000030;

    public N64GsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
        if (IsFileEncrypted())
        {
            Decrypt();
        }

        // TODO(CheatoBaggins): Implement
        Metadata.IsKnownVersion = false;

        // TODO(CheatoBaggins): Decompress v3.x ROM files

        _reader = new BigEndianReader(Bytes);
        _writer = new BigEndianWriter(Bytes);

        _headerId = _reader.ReadCStringAt(HeaderIdAddr, 0x10);
        _rawTimestamp = _reader.ReadPrintableCStringAt(BuildTimestampAddr, 15);

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

        _isV3Firmware        = _reader.ReadUInt32(0x00001000) == 0x00000000;
        _isV1GameList        = _reader.ReadUInt32(0x0002DFF0) == 0x00000000;
        _isV3KeyCodeListAddr = _reader.ReadUInt32(0x0002FBF0) == 0xFFFFFFFF;
        _keyCodeListAddr     = (uint)(_isV3KeyCodeListAddr ? 0x0002FC00 : 0x0002D800);
        _supportsKeyCodes    = _reader.ReadUInt32(_keyCodeListAddr) != 0x00000000;
        _supportsUserPrefs   = _reader.ReadUInt32(0x0002FAF0) == 0xFFFFFFFF;
        _firmwareAddr        = (uint)(_isV3Firmware ? 0x00001080 : 0x00001000);
        _gameListAddr        = (uint)(_isV1GameList ? 0x0002E000 : 0x00030000);
        _userPrefsAddr       = _supportsUserPrefs ? 0x0002FB00 : 0xFFFFFFFF;

        List<KeyCode> keyCodes = ReadKeyCodes();
        _activeKeyCode = ReadActiveKeyCode(keyCodes);
        _keyCodes = keyCodes;

        Games = ReadGames();
    }

    public override bool FormatSupportsFirmwareCompression()
    {
        return true;
    }

    private List<Game> ReadGames()
    {
        List<Game> games = new List<Game>();
        Seek(_gameListAddr);
        int gamesCount = _reader.ReadSInt32();
        for (int gameIndex = 0; gameIndex < gamesCount; gameIndex++)
        {
            games.Add(ReadGame());
        }
        return games;
    }

    private Game ReadGame()
    {
        Game game = Game.NewGame(ReadName());
        int cheatCount = _reader.ReadUByte();
        for (int cheat = 0; cheat < cheatCount; cheat++)
        {
            ReadCheat(game);
        }
        return game;
    }

    private void ReadCheat(Game game)
    {
        Cheat cheat = game.AddCheat(ReadName());
        int codeCount = _reader.ReadUByte();
        bool cheatOn = (codeCount & 0x80) > 0;
        codeCount &= 0x7F;
        cheat.IsActive = cheatOn;
        for (int code = 0; code < codeCount; code++)
        {
            ReadCode(cheat);
        }
    }

    private void ReadCode(Cheat cheat)
    {
        uint address = _reader.ReadUInt32();
        int value = _reader.ReadUInt16();

        cheat.AddCode(address, value);
    }

    private string ReadName()
    {
        uint pos = _reader.Position;

        // Firmware does not support names longer than 30 chars.
        string name = _reader.ReadCStringAt(pos, 30).Value;

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
        byte[] crcBytes = _reader.PeekBytesAt(ActiveKeyCodeAddr, 8);
        byte[] pcBytes = _reader.PeekBytesAt(ProgramCounterAddr, 4);

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
        byte[] activePrefix = _reader.PeekBytesAt(ActiveKeyCodeAddr, 8);

        Seek(_keyCodeListAddr);
        byte[] listBytes = _reader.PeekBytes(0xA0);
        uint maxPos = _reader.Position + (uint)listBytes.Length;
        int keyCodeByteLength = listBytes.Find("Mario World 64 & Others");

        // Valid key codes are either 9 or 13 bytes long.
        if (keyCodeByteLength < 9)
        {
            return new List<KeyCode>();
        }

        var keyCodes = new List<KeyCode>();
        while (_reader.Position <= maxPos)
        {
            byte[] bytes = _reader.ReadBytes((uint)keyCodeByteLength);
            RomString name = _reader.ReadPrintableCString(0x1F);
            while (_reader.PeekBytes(1)[0] == 0)
            {
                _reader.ReadUByte();
            }
            var isActive = bytes.Contains(activePrefix);
            var keyCode = new KeyCode(name.Value, bytes, isActive);
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

        var version = N64GsVersion.From(_rawTimestamp.Value)?
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
        int titleVersionPos = haystack.Find(needle);
        _isCompressed = titleVersionPos == -1;
        if (_isCompressed)
        {
            return null;
        }

        // Uncomment to return ONLY the number (e.g., "2.21")
        // titleVersionPos += needle.Length;

        return _reader.ReadPrintableCStringAt((uint)titleVersionPos, needle.Length + 5).Trim();
    }

    private N64GsRom Seek(uint address)
    {
        _reader.Seek(address);
        _writer.Seek(address);
        return this;
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
        for (int i = 0; i < Bytes.Length; i++)
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
        bool is256KiB = bytes.Length == 0x00040000;
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
        var firmwareAddr = $"0x{_firmwareAddr:X8}";
        var gameListAddr = $"0x{_gameListAddr:X8}";
        var keyCodeListAddr = _supportsKeyCodes ? $"0x{_keyCodeListAddr:X8}" : "Not supported";
        var userPrefsAddr = (_supportsUserPrefs ? $"0x{_userPrefsAddr:X8}" : "Not supported");
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
