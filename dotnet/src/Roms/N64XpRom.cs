using System.Globalization;
using System.Text.RegularExpressions;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.N64;

// ReSharper disable BuiltInTypeReferenceStyle

namespace LibreShark.Hammerhead.Roms;

using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using f64 = Double;

/// <summary>
/// Xplorer 64 for Nintendo 64,
/// made by Blaze and Future Console Design (FCD).
/// </summary>
public sealed class N64XpRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.N64Xplorer64;
    private const u32 GameListAddr = 0x00030000;

    private readonly BigEndianReader _reader;
    private readonly BigEndianWriter _writer;

    public N64XpRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisRomFormat)
    {
        if (IsFileScrambled())
        {
            Unscramble();
        }

        _reader = new BigEndianReader(Bytes);
        _writer = new BigEndianWriter(Bytes);

        Metadata.Brand = RomBrand.Xplorer;

        // TODO(CheatoBaggins): Implement
        Metadata.IsKnownVersion = false;

        RomString versionRaw = _reader.ReadCStringAt(0x17, 5);
        RomString languageRaw = _reader.ReadCStringAt(0x1C, 1);
        RomString buildRaw = _reader.ReadCStringAt(0x20);
        RomString countryRaw = _reader.ReadCStringAt(0x28);

        // E.g.:
        // - "1.000E build 1772" -> 10_001_772
        // - "1.000E build 1834" -> 10_001_834
        // - "1.067G build 1930" -> 10_671_930
        // - "1.067E build 2510" -> 10_672_510
        // - "1.067E build 2515" -> 10_672_515
        Metadata.SortableVersion = f64.Parse(versionRaw.Value) * 10000000 + s32.Parse(buildRaw.Value);
        Metadata.DisplayVersion = $"v{versionRaw.Value}{languageRaw.Value} build {buildRaw.Value} ({countryRaw.Value})";
        Metadata.LanguageIetfCode = GetIetfCode(languageRaw, countryRaw);

        RomString firstLine = _reader.ReadCStringAt(0x0);
        Metadata.Identifiers.Add(firstLine);
        Metadata.Identifiers.Add(versionRaw);
        Metadata.Identifiers.Add(languageRaw);
        Metadata.Identifiers.Add(buildRaw);
        Metadata.Identifiers.Add(countryRaw);
        RomString fcd = _reader.ReadCStringAt(0x40);
        RomString greetz = _reader.ReadCStringAt(0x800);
        RomString develop = _reader.ReadCStringAt(0x8A0);
        RomString peeps = _reader.ReadCStringAt(0x900);
        RomString link = _reader.ReadCStringAt(0x940);
        Metadata.Identifiers.Add(fcd);
        Metadata.Identifiers.Add(greetz);
        Metadata.Identifiers.Add(develop);
        Metadata.Identifiers.Add(peeps);
        Metadata.Identifiers.Add(link);

        ReadBuildDate(out RomString buildDateRaw, out string buildDateIso, out RomString wayneStr);
        Metadata.BuildDateIso = buildDateIso;
        Metadata.Identifiers.Add(wayneStr);
        Metadata.Identifiers.Add(buildDateRaw);

        _reader.Seek(GameListAddr);
        while (!_reader.IsSectionPadding())
        {
            RomString gameName = _reader.ReadCString();
            u8 cheatCount = _reader.ReadU8();

            Game game = new(gameName.Value);

            for (u16 cheatIdx = 0; cheatIdx < cheatCount; cheatIdx++)
            {
                RomString cheatName = _reader.ReadCString();
                u8 codeCount = _reader.ReadU8();

                Cheat cheat = new()
                {
                    Name = cheatName.Value,
                    IsActive = false,
                };

                for (u16 codeIdx = 0; codeIdx < codeCount; codeIdx++)
                {
                    u32 address = _reader.ReadU32();
                    u16 value = _reader.ReadU16();
                    cheat.AddCode(address, value);
                }

                game.AddCheat(cheat);
            }

            Games.Add(game);
        }
    }

    private string GetIetfCode(RomString languageRaw, RomString countryRaw)
    {
        switch (languageRaw.Value)
        {
            case "E":
                return "en-GB";
            case "G":
                return "de-DE";
        }
        // Undetermined (unknown)
        return "und";
    }

    private void ReadBuildDate(out RomString buildDateRaw, out string buildDateIso, out RomString wayneStr)
    {
        u32 waynePos = (u32)Bytes.Find("Wayne Hughes Beckett!");
        wayneStr = _reader.ReadCStringAt(waynePos);
        u32 buildDatePos = waynePos + 0x40;
        buildDateRaw = _reader.ReadCStringAt(buildDatePos);
        Match match = Regex.Match(buildDateRaw.Value,
            @"(?<ddd>\w{3}) (?<MMM>\w{3}) (?<d>\d{1,2}) (?<H>\d{1,2}):(?<mm>\d{2}):(?<ss>\d{2}) (?<ZZZ>\w{2,3}) (?<yyyy>\d{4})");
        if (!match.Success)
        {
            throw new FormatException(
                $"Build date/time stamp '{buildDateRaw.Value}' at {buildDateRaw.Addr} " +
                "does not match expected format `ddd MMM d H:mm:ss ZZZ yyyy`. " +
                "See https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
        }

        string ddd = match.Groups["ddd"].Value;
        string MMM = match.Groups["MMM"].Value;
        string d = match.Groups["d"].Value;
        string H = match.Groups["H"].Value;
        string mm = match.Groups["mm"].Value;
        string ss = match.Groups["ss"].Value;
        string ZZZ = match.Groups["ZZZ"].Value;
        // British Summer Time (BST)
        if (ZZZ == "BST")
        {
            ZZZ = "GMT";
        }
        string yyyy = match.Groups["yyyy"].Value;
        const string dateTimeFormat =
            // Wed Nov 24 15:25:52 GMT 1999
            "ddd MMM d H:mm:ss yyyy";
        string buildDateFixed = $"{ddd} {MMM} {d} {H}:{mm}:{ss} {yyyy}";
        DateTimeOffset buildDateTimeWithoutTz = DateTimeOffset.ParseExact(
            buildDateFixed, dateTimeFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.None);
        TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById(ZZZ);
        DateTimeOffset cetTime = TimeZoneInfo.ConvertTime(buildDateTimeWithoutTz, tzInfo);
        DateTimeOffset buildDateTimeWithTz = buildDateTimeWithoutTz
            .Subtract(cetTime.Offset)
            .ToOffset(cetTime.Offset);
        buildDateIso = buildDateTimeWithTz.ToIsoString();
    }

    public override bool FormatSupportsFileScrambling()
    {
        return true;
    }

    public override bool IsFileScrambled()
    {
        return DetectScrambled(InitialBytes.ToArray());
    }

    private void Unscramble()
    {
        byte[] unscrambled = N64XpScrambler.UnscrambleXpRom(Bytes);
        Array.Copy(unscrambled, Bytes, unscrambled.Length);
    }

    public byte[] GetPlain()
    {
        // Return a copy of the array to prevent the caller from mutating
        // internal state.
        return Bytes.ToArray();
    }

    public byte[] GetScrambled()
    {
        return N64XpScrambler.ScrambleXpRom(Bytes);
    }

    private static bool DetectPlain(byte[] bytes)
    {
        var idBytes = bytes[0x40..0x55];
        var idStr = idBytes.ToAsciiString();
        return idStr == "FUTURE CONSOLE DESIGN";
    }

    private static bool DetectScrambled(byte[] bytes)
    {
        byte[] maybeScrambledFcdBytes =
        {
            bytes[0x131E], bytes[0x131F],
            bytes[0x133E], bytes[0x133F],
            bytes[0x171E], bytes[0x171F],
            bytes[0x167E], bytes[0x167F],
            bytes[0x031E], bytes[0x031F],
            bytes[0x033E], bytes[0x033F],
            bytes[0x071E], bytes[0x071F],
            bytes[0x073E], bytes[0x073F],
            bytes[0x139E], bytes[0x139F],
            bytes[0x13BE], bytes[0x13BF],
            bytes[0x179E],
        };
        byte[] expectedFcdBytes = "FUTURE CONSOLE DESIGN"u8.ToArray();
        bool isFirstEqual = expectedFcdBytes[0x00..0x05]
            .SequenceEqual(maybeScrambledFcdBytes[0x00..0x05]);
        bool isSecondEqual = expectedFcdBytes[0x08..0x14]
            .SequenceEqual(maybeScrambledFcdBytes[0x08..0x14]);
        return isFirstEqual && isSecondEqual;
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.Length == 0x00040000;
        return is256KiB && (DetectPlain(bytes) || DetectScrambled(bytes));
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
    }
}
