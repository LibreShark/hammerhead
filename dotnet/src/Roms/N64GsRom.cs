using LibreShark.Hammerhead.N64;

namespace LibreShark.Hammerhead;

/// <summary>
/// GameShark (USA/CAN), Action Replay (UK/EU), Equalizer (UK/EU), and Game Buster (Germany) for
/// Nintendo 64, made by Datel/InterAct.
/// </summary>
public sealed class N64GsRom : Rom
{
    private const RomType ThisRomType = RomType.N64Gameshark;

    private readonly N64GsBinReader _reader;
    private readonly N64GsBinWriter _writer;

    private bool _isCompressed;
    private bool _isV3Firmware;
    private bool _isV1GameList;
    private bool _isV1KeyCodeList;
    private bool _isV3KeyCodeList;
    private bool _supportsUserPrefs;
    private bool _supportsKeyCodes;

    private uint _firmwareAddr;
    private uint _gameListAddr;
    private uint _keyCodeListAddr;
    private uint _userPrefsAddr;

    private const uint ProgramCounterAddr = 0x00000008;
    private const uint ActiveKeyCodeAddr  = 0x00000010;
    private const uint BuildTimestampAddr = 0x00000030;

    public N64GsRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomType)
    {
        if (IsEncrypted())
        {
            Decrypt();
        }

        // TODO(CheatoBaggins): Decompress v3.x ROM files

        _reader = new N64GsBinReader(Bytes);
        _writer = new N64GsBinWriter(Bytes);

        Parse();
    }

    private void Parse()
    {
        var headerId = _reader.ReadCStringAt(0x20, 0x10);
        Metadata.Identifiers.Add(headerId);

        var rawTimestamp = _reader.ReadPrintableCStringAt(BuildTimestampAddr, 15);
        Metadata.Identifiers.Add(rawTimestamp);

        // TODO(CheatoBaggins): Decompress v2.5+ firmware before scanning
        RomString? titleVersionNumberStr = ReadTitleVersion("N64 GameShark Version ") ??
                                           ReadTitleVersion("N64 Action Replay Version ") ??
                                           ReadTitleVersion("N64 Equalizer Version ") ??
                                           ReadTitleVersion("N64 Game Buster Version ");

        if (titleVersionNumberStr != null)
        {
            Metadata.Identifiers.Add(titleVersionNumberStr);
        }

        var version = N64GsRomVersion.From(rawTimestamp.Value)?.WithTitleVersionNumber(titleVersionNumberStr?.Value);
        if (version == null)
        {
            throw new InvalidDataException("Failed to find N64 GameShark ROM version!");
        }

        Metadata.Brand = version.Brand;
        Metadata.BuildDateRaw = rawTimestamp;
        Metadata.BuildDateIso = version.DisplayBuildTimestampIso;
        Metadata.DisplayVersion = version.DisplayNumber;
        Metadata.SortableVersion = version.Number;
        Metadata.LanguageIetfCode = version.Locale.Name;

        _isV3Firmware      = _reader.ReadUInt32(0x00001000) == 0x00000000;
        _isV1GameList      = _reader.ReadUInt32(0x0002DFF0) == 0x00000000;
        _isV1KeyCodeList   = _reader.ReadUInt32(0x0002D7F0) == 0x00000000;
        _isV3KeyCodeList   = _reader.ReadUInt32(0x0002FBF0) == 0xFFFFFFFF;
        _supportsUserPrefs = _reader.ReadUInt32(0x0002FAF0) == 0xFFFFFFFF;
        _firmwareAddr      = (uint)(_isV3Firmware ? 0x00001080 : 0x00001000);
        _gameListAddr      = (uint)(_isV1GameList ? 0x0002E000 : 0x00030000);
        _keyCodeListAddr   = _isV3KeyCodeList ? 0x0002FC00 : _isV1KeyCodeList ? 0x0002D800 : 0xFFFFFFFF;
        _supportsKeyCodes  = _keyCodeListAddr != 0xFFFFFFFF;
        _userPrefsAddr     = _supportsUserPrefs ? 0x0002FB00 : 0xFFFFFFFF;
    }

    private RomString? ReadTitleVersion(string needle)
    {
        byte[] haystack = Bytes[..0x30000];
        int titleVersionPos = haystack.Find(needle);
        _isCompressed = titleVersionPos == -1;

        if (!_isCompressed)
        {
            titleVersionPos += needle.Length;
            // e.g., "2.21"
            return _reader.ReadPrintableCStringAt((uint)titleVersionPos, 5).Trim();
        }

        return null;
    }

    private N64GsRom Seek(uint address)
    {
        _reader.Seek(address);
        _writer.Seek(address);
        return this;
    }

    public override bool IsEncrypted()
    {
        return DetectEncrypted(InitialBytes.ToArray());
    }

    public override bool IsCompressed()
    {
        return _isCompressed;
    }

    private void Decrypt()
    {
        if (!DetectEncrypted(Bytes))
        {
            return;
        }
        // TODO(CheatoBaggins): Implement
        Console.WriteLine("ENCRYPTED!");
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
        return rom.Metadata.Type == ThisRomType;
    }

    public static bool Is(RomType type)
    {
        return type == ThisRomType;
    }

    public override void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"N64 GameShark ROM file: '{Metadata.FilePath}'");
        Console.WriteLine();
        Console.WriteLine($"Encrypted: {IsEncrypted()}");
        Console.WriteLine($"Compressed: {IsCompressed()}");
        Console.WriteLine();
        Console.WriteLine($"Type: {Metadata.Type.ToDisplayString()}");
        Console.WriteLine($"Brand: {Metadata.Brand.ToDisplayString()}");
        Console.WriteLine($"Locale: {Metadata.LanguageIetfCode}");
        Console.WriteLine($"Version: {Metadata.DisplayVersion}");
        Console.WriteLine($"Build date: {Metadata.BuildDateIso}");
        Console.WriteLine();
        Console.WriteLine("Identifiers:");
        foreach (var id in Metadata.Identifiers)
        {
            Console.WriteLine($"{id.Addr.ToDisplayString()} = '{id.Value}'");
        }
        Console.WriteLine();
        var fwType = $"0x{_firmwareAddr:X8}";
        var gameListType = $"0x{_gameListAddr:X8}";
        var keyCodeListType = _supportsKeyCodes ? $"0x{_keyCodeListAddr:X8}" : "Not supported";
        var userPrefsType = (_supportsUserPrefs ? $"0x{_userPrefsAddr:X8}" : "Not supported");
        Console.WriteLine($"Firmware:   {fwType}");
        Console.WriteLine($"Game list:  {gameListType}");
        Console.WriteLine($"User prefs: {userPrefsType}");
        Console.WriteLine($"Key codes:  {keyCodeListType}");
    }
}
