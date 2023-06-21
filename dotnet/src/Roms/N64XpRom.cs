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
using s64 = Int64;
using u64 = UInt64;
using f64 = Double;

/// <summary>
/// Xplorer 64 for Nintendo 64,
/// made by Blaze and Future Console Design (FCD).
/// </summary>
public sealed class N64XpRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.Nintendo64;
    private const RomFormat ThisRomFormat = RomFormat.N64Xplorer64;

    private const u32 GameListAddr = 0x00030000;
    private const u32 UserPrefsAddr = 0x0003F000;
    private const u32 LastGameNameAddr = 0x0003F420;
    private const u32 LastGameCartIdAddr = 0x0003F43C;

    private static readonly string[] KnownIsoBuildDates = {
        "1999-05-07T21:34:19+00:00",
        "1999-08-16T17:10:59+00:00",
        "1999-11-24T00:13:18+00:00",
        "1999-11-24T21:25:52+00:00",
        "2000-05-06T04:42:59+00:00",
    };

    private readonly BigEndianScribe _scribe;

    public N64XpRom(string filePath, byte[] bytes)
        : base(filePath, bytes, ThisConsole, ThisRomFormat)
    {
        if (IsFileScrambled())
        {
            Unscramble();
        }

        _scribe = new BigEndianScribe(Bytes);

        Metadata.Brand = RomBrand.Xplorer;

        // TODO(CheatoBaggins): Implement
        Metadata.IsKnownVersion = false;

        RomString firstLine = _scribe.Seek(0x0).ReadCStringUntilNull();
        RomString versionRaw = _scribe.Seek(0x17).ReadCStringUntilNull(5);
        RomString languageRaw = _scribe.Seek(0x1C).ReadCStringUntilNull(1);
        RomString buildRaw = _scribe.Seek(0x20).ReadCStringUntilNull();
        RomString countryRaw = _scribe.Seek(0x28).ReadCStringUntilNull();
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

        RomString fcd = _scribe.Seek(0x40).ReadCStringUntilNull();
        RomString greetz = _scribe.Seek(0x800).ReadCStringUntilNull();
        RomString develop = _scribe.Seek(0x8A0).ReadCStringUntilNull();
        RomString peeps = _scribe.Seek(0x900).ReadCStringUntilNull();
        RomString link = _scribe.Seek(0x940).ReadCStringUntilNull();
        ReadBuildDate(out RomString buildDateRaw, out string buildDateIso, out RomString wayneStr);

        Metadata.BuildDateIso = buildDateIso;
        Metadata.IsKnownVersion = KnownIsoBuildDates.Contains(buildDateIso);

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
        return _scribe.MaintainPosition(() => !_scribe.Seek(UserPrefsAddr).IsPadding());
    }

    private void ReadUserPrefs()
    {
        _scribe.Seek(UserPrefsAddr);

        if (!HasUserPrefs())
        {
            return;
        }

        // TODO(CheatoBaggins): Decode user preferences

        RomString lastGameName = _scribe.Seek(LastGameNameAddr).ReadCStringUntilNull(20).Trim();
        RomString lastGameCartId = _scribe.Seek(LastGameCartIdAddr).ReadCStringUntilNull(2);
        Metadata.Identifiers.Add(lastGameName);
        Metadata.Identifiers.Add(lastGameCartId);
    }

    private void ReadGames()
    {
        _scribe.Seek(GameListAddr);
        bool stop = false;
        while (!stop && !_scribe.IsPadding())
        {
            RomString gameName = _scribe.ReadCStringUntilNull();
            u8 cheatCount = _scribe.ReadU8();

            N64Game game = new(gameName.Value);

            for (u16 cheatIdx = 0; !stop && cheatIdx < cheatCount; cheatIdx++)
            {
                // TODO(CheatoBaggins): inflate `FA`, etc.
                // E.g.:
                // - "Hybrid Heaven" -> "Infinite `FA`"
                // - "GEX 64" -> "Infinite `F8`"
                // - "Donkey Kong 64 alternativ" -> "Infinite `FA`"
                RomString cheatName = _scribe.ReadCStringUntilNull();
                u8 codeCount = _scribe.ReadU8();

                if (cheatName.Value.Length == 0)
                {
                    Console.WriteLine($"{cheatName.Addr.ToDisplayString()}: Empty cheat name!");
                    stop = true;
                    break;
                }

                N64Cheat cheat = new(cheatName.Value)
                {
                    IsActive = false,
                };

                for (u16 codeIdx = 0; codeIdx < codeCount; codeIdx++)
                {
                    byte[] codeBytes = _scribe.ReadBytes(6);
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

                    BigEndianScribe codeScribe = new BigEndianScribe(codeBytes);
                    byte[] address = codeScribe.ReadBytes(4);
                    byte[] value = codeScribe.ReadBytes(2);
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
        wayneStr = _scribe.Seek(waynePos).ReadCStringUntilNull();
        u32 buildDatePos = waynePos + 0x40;
        buildDateRaw = _scribe.Seek(buildDatePos).ReadCStringUntilNull();
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
        DateTimeOffset buildDateTimeWithTz = buildDateTimeWithoutTz.WithTimeZone(ZZZ);
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
        return idStr is
            "Future Console Design" or
            "FUTURE CONSOLE DESIGN";
    }

    private static string SS(byte[] bytes, u32 addr)
    {
        return bytes[(int)(addr)..(int)(addr + 2)].ToAsciiString();
    }

    private static bool DetectScrambled(byte[] bytes)
    {
        string strle = SS(bytes, 0x0016);
        string strFC = SS(bytes, 0x0436);
        string strDS = SS(bytes, 0x1096);
        string strXp = SS(bytes, 0x121C);
        string strlo = SS(bytes, 0x123C);
        string strFU = SS(bytes, 0x131E);
        string strTU = SS(bytes, 0x133E);
        return strle == "le" &&
               strFC == "FC" &&
               strDS == "D " &&
               strXp == "Xp" &&
               strlo == "lo" &&
               strFU is "Fu" or "FU" &&
               strTU is "tu" or "TU" &&
               true;
    }

    public static bool Is(byte[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
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
