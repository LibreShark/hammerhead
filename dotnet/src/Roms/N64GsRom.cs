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
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        return is256KiB && (DetectDecrypted(bytes) || DetectEncrypted(bytes));
    }

    private static bool DetectDecrypted(byte[] bytes)
    {
        byte[] first4Bytes = bytes[..4];
        bool isN64 = first4Bytes.SequenceEqual(new byte[] { 0x80, 0x37, 0x12, 0x40 }) ||
                     first4Bytes.SequenceEqual(new byte[] { 0x80, 0x37, 0x12, 0x00 });
        const string v1or2Header = "(C) DATEL D&D ";
        const string v3ProHeader = "(C) MUSHROOM &";
        return isN64 && (bytes.Contains(v1or2Header) || bytes.Contains(v3ProHeader));
    }

    private static bool DetectEncrypted(byte[] bytes)
    {
        // TODO(CheatoBaggins): Implement
        return false;
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
    }
}
