using System.Globalization;
using System.Text.RegularExpressions;

namespace LibreShark.Hammerhead.N64;

public partial class RomVersion
{
    public readonly string Raw;
    public readonly double Number;
    public readonly DateTime BuildTimestamp;

    private readonly string? _disambiguator;

    private RomVersion(string raw, double number, string? disambiguator, DateTime buildTimestamp)
    {
        Raw = raw;
        Number = number;
        _disambiguator = disambiguator;
        BuildTimestamp = buildTimestamp;
    }

    public static RomVersion? From(string raw)
    {
        return KnownVersion(raw) ?? UnknownVersion(raw);
    }

    private static RomVersion Of(string raw, double number, string? disambiguator, DateTime buildTimestamp)
    {
        return new RomVersion(raw, number, disambiguator, buildTimestamp);
    }

    private static RomVersion Of(string raw, double number, string? disambiguator,
        int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
    {
        return new RomVersion(raw, number, disambiguator, new DateTime(year, month, day, hour, minute, second));
    }

    private static RomVersion? KnownVersion(string raw)
    {
        // TODO: Find a v2.20 ROM and add its build timestamp here
        return raw.Trim() switch
        {
            // Action Replay
            "14:56 Apr 15 98" => Of(raw, 1.11, "AR",       1998, 04, 15, 14, 56),
            "15:50 Mar 24 99" => Of(raw, 3.00, "AR",       1999, 03, 24, 15, 50),
            "16:08 Apr 18"    => Of(raw, 3.30, "AR",       2000, 04, 18, 16, 08),

            // GameShark
            "10:35 Aug 19 97" => Of(raw, 1.04, null,       1997, 08, 19, 10, 35),
            "16:25 Sep 4 97"  => Of(raw, 1.05, null,       1997, 09, 04, 16, 25),
            "14:25 Sep 19 97" => Of(raw, 1.06, null,       1997, 09, 19, 14, 25),
            "10:24 Nov 7 97"  => Of(raw, 1.07, null,       1997, 11, 07, 10, 24),
            "11:58 Nov 24 97" => Of(raw, 1.08, "November", 1997, 11, 24, 11, 58),
            "11:10 Dec 8 97"  => Of(raw, 1.08, "December", 1997, 12, 08, 11, 10),
            "17:40 Jan 5 98"  => Of(raw, 1.09, null,       1998, 01, 05, 17, 40),
            "08:06 Mar 5 98"  => Of(raw, 2.00, "March",    1998, 03, 05, 08, 06),
            "10:05 Apr 6 98"  => Of(raw, 2.00, "April",    1998, 04, 06, 10, 05),
            "13:57 Aug 25 98" => Of(raw, 2.10, null,       1998, 08, 25, 13, 57),
            "12:47 Dec 18 98" => Of(raw, 2.21, null,       1998, 12, 18, 12, 47),
            "12:58 May 4"     => Of(raw, 2.50, null,       1998, 12, 18, 12, 58),
            "15:05 Apr 1 99"  => Of(raw, 3.00, null,       1999, 04, 01, 15, 05),
            "16:50 Jun 9 99"  => Of(raw, 3.10, null,       1999, 06, 09, 16, 50),
            "18:45 Jun 22 99" => Of(raw, 3.20, null,       1999, 06, 22, 18, 45),
            "14:26 Jan 4"     => Of(raw, 3.21, null,       2000, 01, 04, 14, 26),
            "09:54 Mar 27"    => Of(raw, 3.30, "March",    2000, 03, 27, 09, 54),
            "15:56 Apr 4"     => Of(raw, 3.30, "April",    2000, 04, 04, 15, 56),

            // Trainers
            "2003 iCEMARi0"   => Of(raw, 1.00, "Perfect Trainer 1.0b", 2003, 06, 18, 00, 00),

            // Unknown
            _                 => null
        };
    }

    [GeneratedRegex("(?<HH>\\d\\d):(?<mm>\\d\\d) (?<MMM>\\w\\w\\w) (?<dd>\\d\\d?)(?: (?<yy>\\d\\d)?)?")]
    private static partial Regex TimestampRegex();

    private static RomVersion? UnknownVersion(string raw)
    {
        string trimmed = raw.Trim();
        var match = TimestampRegex().Match(trimmed);
        if (!match.Success)
        {
            Console.Error.WriteLine($"ERROR: Invalid GS ROM build timestamp: '{trimmed}'(len = {trimmed.Length}). Expected HH:mm MMM dd [yy].");
            return null;
        }

        var HH = match.Groups["HH"].Value;
        var mm = match.Groups["mm"].Value;
        var MMM = match.Groups["MMM"].Value;
        var dd = match.Groups["dd"].Value;

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

        return Of(raw, 0.00, "UNKNOWN", timestamp);
    }

    private static bool Is(string rawDateTime, string dateTimeFormat, out DateTime datetime)
    {
        return DateTime.TryParseExact(rawDateTime, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out datetime);
    }

    public override string ToString()
    {
        return $"v{Number:F2}" +
               (string.IsNullOrEmpty(_disambiguator) ? "" : $" ({_disambiguator})") +
               $", built on {BuildTimestamp:yyyy-MM-dd HH:mm} ('{Raw}')";
    }
}
