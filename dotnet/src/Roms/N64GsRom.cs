using System.Collections.Immutable;
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

    private bool _isCompressed = false;

    private bool _isV3Firmware;
    private bool _isV2GameList;
    private bool _isV2KeyCodeList;
    private bool _isV3KeyCodeList;
    private bool _hasUserConfig;
    private uint _firmwareAddr;
    private uint _gameListAddr;
    private uint _keyCodeListAddr;

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

        SeekBuildTimestamp();
        var rawTimestamp = _reader.ReadPrintableCString(15);
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

        // if (find(0x00000000, 0x00000007, 0xAE, 0x59, 0x63, 0x54) == 0) {
        //     u8 encryptedBytes[0x00040000] @ 0x00000000;
        // } else {
        //     HeaderSection header @ 0x00000000;
        //
        _isV3Firmware    = _reader.ReadUInt32(0x00001000) == 0x00000000;
        _isV2GameList    = _reader.ReadUInt32(0x0002DFF0) == 0x00000000;
        _isV2KeyCodeList = _reader.ReadUInt32(0x0002D7F0) == 0x00000000;
        _isV3KeyCodeList = _reader.ReadUInt32(0x0002FBF0) == 0xFFFFFFFF;
        _hasUserConfig   = _reader.ReadUInt32(0x0002FAF0) == 0xFFFFFFFF;
        _firmwareAddr    = (uint)(_isV3Firmware ? 0x00001080 : 0x00001000);
        _gameListAddr    = (uint)(_isV2GameList ? 0x0002E000 : 0x00030000);
        _keyCodeListAddr = _isV3KeyCodeList ? 0x0002FC00 : _isV2KeyCodeList ? 0x0002D800 : 0xFFFFFFFF;

        //     FirmwareSection firmware @ firmwareAddr;
        //     GameListSection gameList @ gameListAddr;
        //
        //     std::print("");
        //
        //     if (keyCodeListAddr != 0xFFFFFFFF) {
        //         KeyCodeListSection keyCodeList @ keyCodeListAddr;
        //     }
        //
        //     if (!(keyCodeList.numKeyCodes > 0)) {
        //         std::print("No key codes found.");
        //     }
        //
        //     if (hasUserConfig) {
        //         UserConfigSection userConfig @ 0x0002FB00;
        //     }
    }

    private RomString? ReadTitleVersion(string needle)
    {
        byte[] haystack = Bytes[..0x30000];
        int titleVersionPos = haystack.Find(needle);
        if (titleVersionPos > -1)
        {
            titleVersionPos += needle.Length;
            Seek(titleVersionPos);
            // e.g., "2.21"
            return _reader.ReadPrintableCString(5).Trim();
        }

        _isCompressed = true;
        return null;
    }

    private N64GsRom SeekGamesList()
    {
        // TODO(CheatoBaggins): Implement
        // Seek(ReadVersion()?.Number >= 2.5 ? 0x00030000 : 0x0002E000);
        return this;
    }

    private N64GsRom SeekStart()
    {
        Seek(0x00000000);
        return this;
    }

    private N64GsRom SeekBuildTimestamp()
    {
        Seek(0x00000030);
        return this;
    }

    private N64GsRom SeekProgramCounter()
    {
        Seek(0x00000008);
        return this;
    }

    private N64GsRom SeekActiveKeyCode()
    {
        Seek(0x00000010);
        return this;
    }

    private N64GsRom SeekKeyCodeList()
    {
        // TODO(CheatoBaggins): Implement
        // Seek(ReadVersion()?.Number >= 2.50 ? 0x0002FC00 : 0x0002D800);
        return this;
    }

    private N64GsRom Seek(int address)
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
        Console.WriteLine($"_isV3Firmware:    {_isV3Firmware}");
        Console.WriteLine($"_isV2GameList:    {_isV2GameList}");
        Console.WriteLine($"_isV2KeyCodeList: {_isV2KeyCodeList}");
        Console.WriteLine($"_isV3KeyCodeList: {_isV3KeyCodeList}");
        Console.WriteLine($"_hasUserConfig:   {_hasUserConfig}");
        Console.WriteLine($"_firmwareAddr:    0x{_firmwareAddr:X8}");
        Console.WriteLine($"_gameListAddr:    0x{_gameListAddr:X8}");
        Console.WriteLine($"_keyCodeListAddr: 0x{_keyCodeListAddr:X8}");
    }
}
