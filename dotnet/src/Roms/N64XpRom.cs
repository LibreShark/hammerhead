using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
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
using f64 = Double;

/// <summary>
/// Xplorer 64 for Nintendo 64,
/// made by Blaze and Future Console Design (FCD).
/// </summary>
public sealed class N64XpRom : Rom
{
    private const RomFormat ThisRomFormat = RomFormat.N64Xplorer64;
    private const u32 GameListAddr = 0x00030000;
    private const u32 UserPrefsAddr = 0x0003F000;
    private const u32 LastGameNameAddr = 0x0003F420;
    private const u32 LastGameCartIdAddr = 0x0003F43C;

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

        RomString firstLine = _reader.Seek(0x0).ReadCString();
        RomString versionRaw = _reader.Seek(0x17).ReadCString(5);
        RomString languageRaw = _reader.Seek(0x1C).ReadCString(1);
        RomString buildRaw = _reader.Seek(0x20).ReadCString();
        RomString countryRaw = _reader.Seek(0x28).ReadCString();
        Metadata.Identifiers.Add(firstLine);
        Metadata.Identifiers.Add(versionRaw);
        Metadata.Identifiers.Add(languageRaw);
        Metadata.Identifiers.Add(buildRaw);
        Metadata.Identifiers.Add(countryRaw);

        // E.g.:
        // - "1.000E build 1772" -> 1_000_001_772
        // - "1.000E build 1834" -> 1_000_001_834
        // - "1.067G build 1930" -> 1_067_001_930
        // - "1.067E build 2510" -> 1_067_002_510
        // - "1.067E build 2515" -> 1_067_002_515
        Metadata.SortableVersion = f64.Parse(versionRaw.Value) * 1000000000 + s32.Parse(buildRaw.Value);
        Metadata.DisplayVersion = $"v{versionRaw.Value}{languageRaw.Value} build {buildRaw.Value} ({countryRaw.Value})";
        Metadata.LanguageIetfCode = GetIetfCode(languageRaw, countryRaw);

        RomString fcd = _reader.Seek(0x40).ReadCString();
        RomString greetz = _reader.Seek(0x800).ReadCString();
        RomString develop = _reader.Seek(0x8A0).ReadCString();
        RomString peeps = _reader.Seek(0x900).ReadCString();
        RomString link = _reader.Seek(0x940).ReadCString();
        ReadBuildDate(out RomString buildDateRaw, out string buildDateIso, out RomString wayneStr);

        Metadata.BuildDateIso = buildDateIso;
        Metadata.Identifiers.Add(fcd);
        Metadata.Identifiers.Add(greetz);
        Metadata.Identifiers.Add(develop);
        Metadata.Identifiers.Add(peeps);
        Metadata.Identifiers.Add(link);
        Metadata.Identifiers.Add(wayneStr);
        Metadata.Identifiers.Add(buildDateRaw);

        ReadGames();
        ReadUserPrefs();
    }

    public override bool FormatSupportsFileScrambling()
    {
        return true;
    }

    public override bool FormatSupportsUserPrefs()
    {
        return true;
    }

    public override bool IsFileScrambled()
    {
        return DetectScrambled(InitialBytes.ToArray());
    }

    public override bool HasUserPrefs()
    {
        return _reader.MaintainPosition(() => !_reader.Seek(UserPrefsAddr).IsSectionPadding());
    }

    private void ReadUserPrefs()
    {
        _reader.Seek(UserPrefsAddr);

        if (!HasUserPrefs())
        {
            return;
        }

        // TODO(CheatoBaggins): Decode user preferences

        RomString lastGameName = _reader.Seek(LastGameNameAddr).ReadCString(20).Trim();
        RomString lastGameCartId = _reader.Seek(LastGameCartIdAddr).ReadCString(2);
        Metadata.Identifiers.Add(lastGameName);
        Metadata.Identifiers.Add(lastGameCartId);
    }

    private void ReadGames()
    {
        _reader.Seek(GameListAddr);
        bool stop = false;
        while (!stop && !_reader.IsSectionPadding())
        {
            RomString gameName = _reader.ReadCString();
            u8 cheatCount = _reader.ReadU8();

            Game game = new(gameName.Value);

            for (u16 cheatIdx = 0; !stop && cheatIdx < cheatCount; cheatIdx++)
            {
                // TODO(CheatoBaggins): inflate `FA`, etc.
                // E.g.:
                // - "Hybrid Heaven" -> "Infinite `FA`"
                // - "GEX 64" -> "Infinite `F8`"
                // - "Donkey Kong 64 alternativ" -> "Infinite `FA`"
                RomString cheatName = _reader.ReadCString();
                u8 codeCount = _reader.ReadU8();

                if (cheatName.Value.Length == 0)
                {
                    Console.WriteLine($"{cheatName.Addr.ToDisplayString()}: Empty cheat name!");
                    stop = true;
                    break;
                }

                Cheat cheat = new(cheatName.Value)
                {
                    IsActive = false,
                };

                for (u16 codeIdx = 0; codeIdx < codeCount; codeIdx++)
                {
                    byte[] codeBytes = _reader.ReadBytes(6);
                    string codeStrOld = codeBytes.ToN64CodeString();
                    if (IsCodeEncrypted(codeBytes))
                    {
                        string codeStrNew1 = DecryptCodeMethod1(codeBytes).ToN64CodeString();
                        string codeStrNew2 = DecryptCodeMethod2(codeBytes).ToN64CodeString();
                        // if (codeStrNew1 != codeStrNew2)
                        // {
                        //     Console.WriteLine("-------------------");
                        //     Console.WriteLine($"{cheatName.Value} ({cheatName.Addr})");
                        //     Console.WriteLine("- encrypted: " + codeStrOld);
                        //     Console.WriteLine("- method 1:  " + codeStrNew1);
                        //     Console.WriteLine("- method 2:  " + codeStrNew2);
                        //     Console.WriteLine("-------------------");
                        // }
                        codeBytes = DecryptCodeMethod2(codeBytes);
                    }

                    BigEndianReader codeReader = new BigEndianReader(codeBytes);
                    byte[] address = codeReader.ReadBytes(4);
                    byte[] value = codeReader.ReadBytes(2);
                    cheat.AddCode(address, value);
                }

                game.AddCheat(cheat);
            }

            Games.Add(game);
        }
    }

    private static bool IsCodeEncrypted(IReadOnlyList<byte> code)
    {
        byte opcodeByte = code[0];
        var opcodeEnum = (Xp64Opcode)opcodeByte;
        ImmutableArray<Xp64Opcode> knownOpcodes = Enum.GetValues<Xp64Opcode>().ToImmutableArray();
        bool isUnencrypted = knownOpcodes.Contains(opcodeEnum);
        return !isUnencrypted;
    }

    // https://doc.kodewerx.org/hacking_n64.html#xp_encryption
    private byte[] EncryptCode(byte[] code)
    {
        return new byte[] {};
    }

    /// <summary>
    /// This method does NOT appear to work correctly.
    ///
    /// From https://doc.kodewerx.org/hacking_n64.html#xp_encryption.
    /// </summary>
    private static byte[] DecryptCodeMethod1(IReadOnlyList<byte> code)
    {
        byte a0 = code[0];
        byte a1 = code[1];
        byte a2 = code[2];
        byte a3 = code[3];
        byte d0 = code[4];
        byte d1 = code[5];
        a0 = (byte)((a0 ^ 0x68));
        a1 = (byte)((a1 ^ 0x81) - 0x2B);
        a2 = (byte)((a2 ^ 0x82) - 0x2B);
        a3 = (byte)((a3 ^ 0x83) - 0x2B);
        d0 = (byte)((d0 ^ 0x84) - 0x2B);
        d1 = (byte)((d1 ^ 0x85) - 0x2B);
        return new byte[] {a0, a1, a2, a3, d0, d1};
    }

    /// <summary>
    /// This method appears to work correctly.
    ///
    /// From https://doc.kodewerx.org/hacking_n64.html#xp_encryption.
    /// </summary>
    private static byte[] DecryptCodeMethod2(IReadOnlyList<byte> code)
    {
        byte a0 = code[0];
        byte a1 = code[1];
        byte a2 = code[2];
        byte a3 = code[3];
        byte d0 = code[4];
        byte d1 = code[5];
        a0 = (byte)((a0 ^ 0x68));
        a1 = (byte)((a1 + 0xAB) ^ 0x01);
        a2 = (byte)((a2 + 0xAB) ^ 0x02);
        a3 = (byte)((a3 + 0xAB) ^ 0x03);
        d0 = (byte)((d0 + 0xAB) ^ 0x04);
        d1 = (byte)((d1 + 0xAB) ^ 0x05);
        return new byte[] {a0, a1, a2, a3, d0, d1};
    }

    private static string GetIetfCode(RomString oneLetterLangCode, RomString countryNameRaw)
    {
        return countryNameRaw.Value switch
        {
            "England" => "en-GB",
            "Germany" => "de-DE",
            "France" => "fr-FR",
            "Australia" => "en-AU",
            _ => "und",
        };
    }

    private void ReadBuildDate(out RomString buildDateRaw, out string buildDateIso, out RomString wayneStr)
    {
        u32 waynePos = (u32)Bytes.Find("Wayne Hughes Beckett!");
        wayneStr = _reader.Seek(waynePos).ReadCString();
        u32 buildDatePos = waynePos + 0x40;
        buildDateRaw = _reader.Seek(buildDatePos).ReadCString();
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
        byte[] idBytes = bytes[0x40..0x55];
        string idStr = idBytes.ToAsciiString();
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
