using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Google.Protobuf;
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

    private readonly bool _isScrambled;
    private readonly bool _hasUserPrefs;

    public N64XpRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, Unobfuscate(rawInput), ThisConsole, ThisRomFormat)
    {
        _isScrambled = DetectScrambled(rawInput);
        _hasUserPrefs = Scribe.MaintainPosition(() => !Scribe.Seek(UserPrefsAddr).IsPadding());

        Metadata.Brand = RomBrand.Xplorer;

        // TODO(CheatoBaggins): Implement
        Metadata.IsKnownVersion = false;

        RomString firstLine = Scribe.Seek(0x0).ReadCStringUntilNull();
        RomString versionRaw = Scribe.Seek(0x17).ReadCStringUntilNull(5);
        RomString languageRaw = Scribe.Seek(0x1C).ReadCStringUntilNull(1);
        RomString buildRaw = Scribe.Seek(0x20).ReadCStringUntilNull();
        RomString countryRaw = Scribe.Seek(0x28).ReadCStringUntilNull();
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

        RomString fcd = Scribe.Seek(0x40).ReadCStringUntilNull();
        RomString greetz = Scribe.Seek(0x800).ReadCStringUntilNull();
        RomString develop = Scribe.Seek(0x8A0).ReadCStringUntilNull();
        RomString peeps = Scribe.Seek(0x900).ReadCStringUntilNull();
        RomString link = Scribe.Seek(0x940).ReadCStringUntilNull();
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

    private static BinaryScribe Unobfuscate(u8[] rawInput)
    {
        u8[] output =
            DetectScrambled(rawInput)
                ? N64XpScrambler.UnscrambleXpRom(rawInput)
                : rawInput.ToArray();
        return new BigEndianScribe(output);
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
        return _isScrambled;
    }

    public override bool HasUserPrefs()
    {
        return _hasUserPrefs;
    }

    private void ReadUserPrefs()
    {
        Scribe.Seek(UserPrefsAddr);

        if (!HasUserPrefs())
        {
            return;
        }

        // TODO(CheatoBaggins): Decode user preferences

        RomString lastGameName = Scribe.Seek(LastGameNameAddr).ReadCStringUntilNull(20).Trim();
        RomString lastGameCartId = Scribe.Seek(LastGameCartIdAddr).ReadCStringUntilNull(2);
        Metadata.Identifiers.Add(lastGameName);
        Metadata.Identifiers.Add(lastGameCartId);
    }

    private void ReadGames()
    {
        Scribe.Seek(GameListAddr);
        u32 gameIdx = 0;
        bool stop = false;
        while (!stop && !Scribe.IsPadding())
        {
            RomString gameName = Scribe.ReadCStringUntilNull();
            u8 cheatCount = Scribe.ReadU8();

            var game = new Game()
            {
                GameIndex = gameIdx,
                GameName = gameName,
            };

            for (u16 cheatIdx = 0; !stop && cheatIdx < cheatCount; cheatIdx++)
            {
                // TODO(CheatoBaggins): inflate `FA`, etc.
                // E.g.:
                // - "Hybrid Heaven" -> "Infinite `FA`"
                // - "GEX 64" -> "Infinite `F8`"
                // - "Donkey Kong 64 alternativ" -> "Infinite `FA`"
                RomString cheatName = Scribe.ReadCStringUntilNull();
                u8 codeCount = Scribe.ReadU8();

                if (cheatName.Value.Length == 0)
                {
                    Console.WriteLine($"{cheatName.Addr.ToDisplayString()}: Empty cheat name!");
                    stop = true;
                    break;
                }

                var cheat = new Cheat()
                {
                    CheatIndex = cheatIdx,
                    CheatName = cheatName,
                    IsCheatActive = false,
                };

                for (u16 codeIdx = 0; codeIdx < codeCount; codeIdx++)
                {
                    u8[] codeBytes = Scribe.ReadBytes(6);
                    string codeStrOld = codeBytes.ToCodeString(GameConsole.Nintendo64);
                    if (IsCodeEncrypted(codeBytes))
                    {
                        string codeStrNew1 = DecryptCodeMethod1(codeBytes).ToCodeString(GameConsole.Nintendo64);
                        string codeStrNew2 = DecryptCodeMethod2(codeBytes).ToCodeString(GameConsole.Nintendo64);
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

                    // TODO(CheatoBaggins): Is this redundant?
                    var codeScribe = new BigEndianScribe(codeBytes);
                    u8[] bytes = codeScribe.ReadBytes(6);
                    cheat.Codes.Add(new Code()
                    {
                        CodeIndex = codeIdx,
                        Bytes = ByteString.CopyFrom(bytes),
                    });
                }

                game.Cheats.Add(cheat);
            }

            Games.Add(game);
            gameIdx++;
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
    private u8[] EncryptCode(u8[] code)
    {
        return new u8[] {};
    }

    /// <summary>
    /// This method does NOT appear to work correctly.
    ///
    /// From https://doc.kodewerx.org/hacking_n64.html#xp_encryption.
    /// </summary>
    private static u8[] DecryptCodeMethod1(IReadOnlyList<byte> code)
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
        return new u8[] {a0, a1, a2, a3, d0, d1};
    }

    /// <summary>
    /// This method appears to work correctly.
    ///
    /// From https://doc.kodewerx.org/hacking_n64.html#xp_encryption.
    /// </summary>
    private static u8[] DecryptCodeMethod2(IReadOnlyList<byte> code)
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
        return new u8[] {a0, a1, a2, a3, d0, d1};
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
        u32 waynePos = (u32)Buffer.Find("Wayne Hughes Beckett!");
        wayneStr = Scribe.Seek(waynePos).ReadCStringUntilNull();
        u32 buildDatePos = waynePos + 0x40;
        buildDateRaw = Scribe.Seek(buildDatePos).ReadCStringUntilNull();
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

    public u8[] GetPlain()
    {
        // Return a copy of the array to prevent the caller from mutating
        // internal state.
        return Buffer.ToArray();
    }

    public u8[] GetScrambled()
    {
        return N64XpScrambler.ScrambleXpRom(Buffer);
    }

    private static bool DetectPlain(u8[] bytes)
    {
        u8[] idBytes = bytes[0x40..0x55];
        string idStr = idBytes.ToAsciiString();
        return idStr is
            "Future Console Design" or
            "FUTURE CONSOLE DESIGN";
    }

    private static bool DetectScrambled(u8[] bytes)
    {
        string strle = Get2Chars(bytes, 0x0016);
        string strFC = Get2Chars(bytes, 0x0436);
        string strDS = Get2Chars(bytes, 0x1096);
        string strXp = Get2Chars(bytes, 0x121C);
        string strlo = Get2Chars(bytes, 0x123C);
        string strFU = Get2Chars(bytes, 0x131E);
        string strTU = Get2Chars(bytes, 0x133E);
        return strle == "le" &&
               strFC == "FC" &&
               strDS == "D " &&
               strXp == "Xp" &&
               strlo == "lo" &&
              (strFU is "Fu" or "FU") &&
              (strTU is "tu" or "TU") &&
               true;
    }

    private static string Get2Chars(u8[] bytes, u32 addr)
    {
        return bytes[(int)(addr)..(int)(addr + 2)].ToAsciiString();
    }

    public static bool Is(u8[] bytes)
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
}
