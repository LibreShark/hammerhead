using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Google.Protobuf;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.N64;

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
/// Xplorer 64 for Nintendo 64,
/// made by Blaze and Future Console Design (FCD).
/// </summary>
public sealed class N64XpRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.Nintendo64;
    private const CodecId ThisCodecId = CodecId.N64Xplorer64Rom;

    public static readonly CodecFileFactory Factory = new(Is, Is, ThisCodecId, Create);

    public static N64XpRom Create(string filePath, u8[] rawInput)
    {
        return new N64XpRom(filePath, rawInput);
    }

    private const u32 GameListAddr = 0x00030000;
    private const u32 UserPrefsAddr = 0x0003F000;
    private const u32 LastGameNameAddr = 0x0003F420;
    private const u32 LastGameCartIdAddr = 0x0003F43C;

    private static readonly string[] KnownIsoBuildDates = {
        // v1.000E build 1772 (England)
        "1999-05-07T21:34:19+00:00",

        // v1.000E build 1834 (England)
        "1999-08-16T17:10:59+00:00",

        // v1.067E build 2510 (England)
        "1999-11-24T00:13:18+00:00",

        // v1.067G build 1930 (Germany)
        "1999-11-24T21:25:52+00:00",

        // v1.067E build 2515 (England)
        "2000-05-06T04:42:59+00:00",
    };

    public override CodecId DefaultCheatOutputCodec => CodecId.N64Xplorer64Text;

    private N64XpRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, Unobfuscate(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsKeyCodes = true;
        Support.SupportsFirmware = true;
        Support.SupportsFileScrambling = true;
        Support.SupportsUserPrefs = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;
        Support.IsFileScrambled = DetectScrambled(rawInput);
        Support.HasPristineUserPrefs = Scribe.MaintainPosition(() => Scribe.Seek(UserPrefsAddr).IsPadding());

        // TODO(CheatoBaggins): Detect
        Support.HasKeyCodes = false;

        Metadata.BrandId = BrandId.Xplorer;

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

    private static AbstractBinaryScribe Unobfuscate(u8[] rawInput)
    {
        u8[] output =
            DetectScrambled(rawInput)
                ? N64XpScrambler.UnscrambleRom(rawInput)
                : rawInput.ToArray();
        return new BigEndianScribe(output);
    }

    public override u8[] Scramble()
    {
        return N64XpScrambler.ScrambleRom(Buffer);
    }

    private void ReadUserPrefs()
    {
        RomString lastGameName = Scribe.Seek(LastGameNameAddr).ReadCStringUntilNull(20).Trim();
        RomString lastGameCartId = Scribe.Seek(LastGameCartIdAddr).ReadCStringUntilNull(2);
        Metadata.Identifiers.Add(lastGameName);
        Metadata.Identifiers.Add(lastGameCartId);

        if (Support.HasPristineUserPrefs)
        {
            return;
        }

        // TODO(CheatoBaggins): Decode user preferences

        Scribe.Seek(UserPrefsAddr);
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
                    if (N64XpCrypter.IsCodeEncrypted(codeBytes))
                    {
                        codeBytes = N64XpCrypter.DecryptCodeMethod1(codeBytes);
                    }
                    cheat.Codes.Add(new Code()
                    {
                        CodeIndex = codeIdx,
                        Bytes = ByteString.CopyFrom(codeBytes),
                    });
                }

                game.Cheats.Add(cheat);
            }

            Games.Add(game);
            gameIdx++;
        }
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

    public override AbstractCodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
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

    public static bool Is(AbstractCodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }
}
