using System.Globalization;
using System.Text.RegularExpressions;

namespace LibreShark.Hammerhead.N64;

public partial class N64GsRomVersion
{
    public static readonly CultureInfo ENGLISH_US = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
    public static readonly CultureInfo ENGLISH_UK = CultureInfo.GetCultureInfoByIetfLanguageTag("en-GB");
    public static readonly CultureInfo GERMAN_GERMANY = CultureInfo.GetCultureInfoByIetfLanguageTag("de-DE");
    public static readonly CultureInfo UNKNOWN_LOCALE = CultureInfo.InvariantCulture;

    public readonly bool IsInDatabase;
    public readonly string RawTimestamp;
    public double Number { get; private set; }
    public readonly string? Disambiguator;
    public readonly DateTime BuildTimestamp;
    public RomBrand Brand { get; private set; }
    public CultureInfo Locale { get; private set; }

    public string? RawTitleVersionNumber { get; private set; }
    public double? ParsedTitleVersionNumber => RawTitleVersionNumber != null ? double.Parse(RawTitleVersionNumber) : null;

    public bool HasDisambiguator => !string.IsNullOrEmpty(Disambiguator);
    public string DisplayBrand => Brand.ToDisplayString();
    public string DisplayNumber => HasDisambiguator ? $"v{Number:F2} ({Disambiguator})" : $"v{Number:F2}";
    public string DisplayBuildTimestampIso => BuildTimestamp.ToString("yyyy-MM-ddTHH:mm+0100");
    public string DisplayBuildTimestampRaw => RawTimestamp;
    public string DisplayLocale => Locale.ToString();

    private N64GsRomVersion(string raw, double number, string? disambiguator, DateTime buildTimestamp, RomBrand brand, CultureInfo locale)
    {
        RawTimestamp = raw;
        Number = number;
        Disambiguator = disambiguator;
        BuildTimestamp = buildTimestamp;
        Brand = brand;
        Locale = locale;
        IsInDatabase = Brand != RomBrand.UnknownBrand;
    }

    public static N64GsRomVersion? From(string raw)
    {
        return KnownVersion(raw) ?? UnknownVersion(raw);
    }

    private static N64GsRomVersion Of(string raw, double number, string? disambiguator, DateTime buildTimestamp)
    {
        return new N64GsRomVersion(raw, number, disambiguator, buildTimestamp, RomBrand.UnknownBrand, UNKNOWN_LOCALE);
    }

    private static N64GsRomVersion Of(string raw, double number, string? disambiguator,
        int year, int month, int day, int hour, int minute, int second, RomBrand brand, CultureInfo locale)
    {
        return new N64GsRomVersion(raw, number, disambiguator, new DateTime(year, month, day, hour, minute, second), brand, locale);
    }

    private static N64GsRomVersion? KnownVersion(string raw)
    {
        // TODO: Find a v2.20 ROM and add its build timestamp here
        return raw.Trim() switch
        {
            // Action Replay
            "14:56 Apr 15 98" => Of(raw, 1.11, null,       1998, 04, 15, 14, 56, 00, RomBrand.ActionReplay, ENGLISH_UK),
            "15:50 Mar 24 99" => Of(raw, 3.00, null,       1999, 03, 24, 15, 50, 00, RomBrand.ActionReplay, ENGLISH_UK),
            "16:08 Apr 18"    => Of(raw, 3.30, null,       2000, 04, 18, 16, 08, 00, RomBrand.ActionReplay, ENGLISH_UK),

            // GameShark
            "12:50 Aug 1 97"  => Of(raw, 1.02, null,       1997, 08, 01, 12, 50, 00, RomBrand.Gameshark, ENGLISH_US),
            "10:35 Aug 19 97" => Of(raw, 1.04, null,       1997, 08, 19, 10, 35, 00, RomBrand.Gameshark, ENGLISH_US),
            "16:25 Sep 4 97"  => Of(raw, 1.05, "Thursday", 1997, 09, 04, 16, 25, 00, RomBrand.Gameshark, ENGLISH_US),
            "13:51 Sep 5 97"  => Of(raw, 1.05, "Friday",   1997, 09, 05, 13, 51, 00, RomBrand.Gameshark, ENGLISH_US),
            "14:25 Sep 19 97" => Of(raw, 1.06, null,       1997, 09, 19, 14, 25, 00, RomBrand.Gameshark, ENGLISH_US),
            "17:21 Oct 27 97" => Of(raw, 1.07, "October",  1997, 10, 27, 17, 21, 00, RomBrand.Gameshark, ENGLISH_US),
            "10:24 Nov 7 97"  => Of(raw, 1.07, "November", 1997, 11, 07, 10, 24, 00, RomBrand.Gameshark, ENGLISH_US),
            "11:58 Nov 24 97" => Of(raw, 1.08, "November", 1997, 11, 24, 11, 58, 00, RomBrand.Gameshark, ENGLISH_US),
            "11:10 Dec 8 97"  => Of(raw, 1.08, "December", 1997, 12, 08, 11, 10, 00, RomBrand.Gameshark, ENGLISH_US),
            "17:40 Jan 5 98"  => Of(raw, 1.09, null,       1998, 01, 05, 17, 40, 00, RomBrand.Gameshark, ENGLISH_US),
            "08:06 Mar 5 98"  => Of(raw, 2.00, "March",    1998, 03, 05, 08, 06, 00, RomBrand.Gameshark, ENGLISH_US),
            "10:05 Apr 6 98"  => Of(raw, 2.00, "April",    1998, 04, 06, 10, 05, 00, RomBrand.Gameshark, ENGLISH_US),
            "13:57 Aug 25 98" => Of(raw, 2.10, null,       1998, 08, 25, 13, 57, 00, RomBrand.Gameshark, ENGLISH_US),
            "12:47 Dec 18 98" => Of(raw, 2.21, null,       1998, 12, 18, 12, 47, 00, RomBrand.Gameshark, ENGLISH_US),
            // TODO: Confirm v2.5 build timestamp
            "12:58 May 4"     => Of(raw, 2.50, null,       1999, 05, 04, 12, 58, 00, RomBrand.Gameshark, ENGLISH_US),
            "15:05 Apr 1 99"  => Of(raw, 3.00, null,       1999, 04, 01, 15, 05, 00, RomBrand.Gameshark, ENGLISH_US),
            "16:50 Jun 9 99"  => Of(raw, 3.10, null,       1999, 06, 09, 16, 50, 00, RomBrand.Gameshark, ENGLISH_US),
            "18:45 Jun 22 99" => Of(raw, 3.20, null,       1999, 06, 22, 18, 45, 00, RomBrand.Gameshark, ENGLISH_US),
            "14:26 Jan 4"     => Of(raw, 3.21, null,       2000, 01, 04, 14, 26, 00, RomBrand.Gameshark, ENGLISH_US),
            "09:54 Mar 27"    => Of(raw, 3.30, "March",    2000, 03, 27, 09, 54, 00, RomBrand.Gameshark, ENGLISH_US),
            "15:56 Apr 4"     => Of(raw, 3.30, "April",    2000, 04, 04, 15, 56, 00, RomBrand.Gameshark, ENGLISH_US),

            // Equalizer (UK)
            // According to this, Equalizer was a "budget" version of the Action Replay, and was also sold in the UK:
            // https://www.reddit.com/r/n64/comments/t2hdsh/comment/hymp77l/
            "09:44 Jul 20 99" => Of(raw, 3.00, null,       1999, 07, 20, 09, 44, 00, RomBrand.Equalizer, ENGLISH_UK),

            // Game Buster (Germany)
            "11:09 Aug 5 99"  => Of(raw, 3.21, null,       1999, 08, 05, 11, 09, 00, RomBrand.GameBuster, GERMAN_GERMANY),

            // Unknown
            _                 => null,
        };
    }

    [GeneratedRegex("(?<HH>\\d\\d):(?<mm>\\d\\d) (?<MMM>\\w\\w\\w) (?<dd>\\d\\d?)(?: (?<yy>\\d\\d)?)?")]
    private static partial Regex TimestampRegex();

    private static N64GsRomVersion? UnknownVersion(string raw)
    {
        string trimmed = raw.Trim();
        var match = TimestampRegex().Match(trimmed);
        if (!match.Success)
        {
            Console.Error.WriteLine($"ERROR: Invalid GS ROM build timestamp: '{trimmed}' (len = {trimmed.Length}). Expected HH:mm MMM dd [yy].");
            return null;
        }

        var HH = match.Groups["HH"].Value;
        var mm = match.Groups["mm"].Value;
        var MMM = match.Groups["MMM"].Value;
        var dd = match.Groups["dd"].Value;

        // Equalizer vX.XX contains either a typo or corrupted data.
        // TODO(CheatoBaggins): Dump more Equalizer ROMs for comparison
        if (MMM == "J5l")
        {
            MMM = "Jul";
        }

        // Versions 2.5, 3.21, and 3.3 omit the year from the end of the timestamp.
        // We specifically handle those cases above, but we're still missing dumps of v1.01, v1.02, and v2.03.
        // The missing dumps were likely made in 1997, so we default to that.
        var yyyy = match.Groups["yy"].Success ? $"19{match.Groups["yy"].Value}" : "1997";

        trimmed = $"{HH}:{mm} {MMM} {dd} {yyyy}";
        if (!Is(trimmed, "HH:mm MMM d yyyy", out var timestamp))
        {
            Console.Error.WriteLine($"ERROR: Invalid GS ROM build timestamp: '{trimmed}' (len = {trimmed.Length}). Expected HH:mm MMM dd yyyy.");
            return null;
        }

        return Of(raw, 0.00, "MISSING FROM OUR DATABASE!", timestamp);
    }

    private static bool Is(string rawDateTime, string dateTimeFormat, out DateTime datetime)
    {
        return DateTime.TryParseExact(rawDateTime, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out datetime);
    }

    public override string ToString()
    {
        return $"{Brand.ToDisplayString()} v{Number:F2}" +
               (string.IsNullOrEmpty(Disambiguator) ? "" : $" ({Disambiguator})") +
               $", built on {BuildTimestamp:yyyy-MM-dd HH:mm} ('{RawTimestamp}') - {Locale}";
    }

    public N64GsRomVersion WithTitleVersionNumber(string? titleVersionStr)
    {
        RawTitleVersionNumber = titleVersionStr;
        if (Brand == RomBrand.UnknownBrand && titleVersionStr != null)
        {
            var match = Regex.Match(titleVersionStr, "(?:N64 )?(?<brand>.+) Version (?<vernum>[0-9.]+)");
            if (match.Success)
            {
                string brand = match.Groups["brand"].Value;
                string vernum = match.Groups["vernum"].Value;
                Number = double.Parse(vernum);
                switch (brand)
                {
                    case "GameShark":
                    case "GameShark Pro":
                        Brand = RomBrand.Gameshark;
                        Locale = ENGLISH_US;
                        break;
                    case "Action Replay":
                    case "Action Replay Pro":
                        Brand = RomBrand.ActionReplay;
                        Locale = ENGLISH_UK;
                        break;
                    case "Equalizer":
                        Brand = RomBrand.Equalizer;
                        Locale = ENGLISH_UK;
                        break;
                    case "Game Buster":
                        Brand = RomBrand.GameBuster;
                        Locale = GERMAN_GERMANY;
                        break;
                }
            }
        }
        return this;
    }
}
