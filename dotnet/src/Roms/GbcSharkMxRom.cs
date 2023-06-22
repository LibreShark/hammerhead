using System.Drawing;
using System.Text.RegularExpressions;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using Google.Protobuf.WellKnownTypes;
using LibreShark.Hammerhead.IO;

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
/// Shark MX email client for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcSharkMxRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.GameBoyColor;
    private const RomFormat ThisRomFormat = RomFormat.GbcSharkMx;

    private const u32 ContactsAddr  = 0x0000C060;
    private const u32 RegCodeAddr   = 0x00020000;
    private const u32 SecretPinAddr = 0x0002007C;
    private const u32 MessagesAddr  = 0x00021000;

    private readonly List<Tz> _tzs = new();

    public GbcSharkMxRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
        Metadata.Brand = RomBrand.SharkMx;

        ParseVersion();
        ParseTimeZones();
        ParseContacts();
        ParseRegistrationCode();
        ParseSecretPin();
        ParseMessages();
    }

    private void ParseContacts()
    {
        Scribe.Seek(ContactsAddr);
    }

    private void ParseRegistrationCode()
    {
        Scribe.Seek(RegCodeAddr);
    }

    private void ParseSecretPin()
    {
        Scribe.Seek(SecretPinAddr);
    }

    private void ParseMessages()
    {
        Scribe.Seek(MessagesAddr);
    }

    private void ParseVersion()
    {
        u32 welcomeAddr = (u32)Scribe.Find("Welcome to");
        u32 manufacturerAddr = (u32)Scribe.Find("Shark MX");

        RomString welcomeStr = Scribe.Seek(welcomeAddr).ReadCStringUntilNull().Readable();
        RomString manufacturerStr = Scribe.Seek(manufacturerAddr).ReadCStringUntilNull().Readable();

        Metadata.Identifiers.Add(welcomeStr);
        Metadata.Identifiers.Add(manufacturerStr);

        Match versionMatch = Regex.Match(welcomeStr.Value, @"(?<country>\w+) V(?<version>[\d.]+)");
        if (!versionMatch.Success)
        {
            throw new NotSupportedException("Unable to find version number in Shark MX ROM file!");
        }

        string countryStr = versionMatch.Groups["country"].Value;
        string versionStr = versionMatch.Groups["version"].Value;
        if (countryStr == "US")
        {
            Metadata.LanguageIetfCode = "en-US";
        }
        else
        {
            Console.Error.WriteLine($"UNKNOWN LANGUAGE FOR COUNTRY '{countryStr}'");
        }

        Metadata.SortableVersion = Double.Parse(versionStr);
        Metadata.DisplayVersion = $"v{Metadata.SortableVersion:F2} ({countryStr})";
    }

    private void ParseTimeZones()
    {
        s32 tzListPos = Scribe.Seek(0).Find("Anchorage");
        if (tzListPos < 0)
        {
            Console.Error.WriteLine("Unable to find time zones!");
        }

        u32 tzListAddr = (u32)tzListPos - 5;
        Scribe.Seek(tzListAddr);
        u8 tzIdx = 0;
        while (true)
        {
            RomString utcOffset = Scribe.ReadCStringUntilNull(3, false).Readable();
            if (!utcOffset.Value.All(IsTzChar))
            {
                break;
            }

            u8[] unknownBytes = Scribe.ReadBytes(2);
            RomString tzName = Scribe.ReadCStringUntilNull(10, true).Readable();
            var tz = new Tz(tzIdx, utcOffset, tzName);
            _tzs.Add(tz);
            tzIdx++;
        }
    }

    private static bool IsTzChar(char c)
    {
        return (c is ' ' or '+' or '-') ||
               (c is >= '0' and <= '9');
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        return is256KiB && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        return bytes.Contains("Shark MX") &&
               bytes.Contains("Datel Design LTD");
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.Format == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    private static BinaryScribe MakeScribe(u8[] rawInput)
    {
        return new LittleEndianScribe(rawInput.ToArray());
    }

    protected override void PrintCustomBody()
    {
        Console.WriteLine();
        PrintHeading($"Time zones ({_tzs.Count})");
        Console.WriteLine(BuildTimeZoneTable());
    }

    protected override void PrintCustomHeader()
    {
    }

    private string BuildTimeZoneTable()
    {
        var headerFormat = new CellFormat()
        {
            Alignment = Alignment.Left,
            FontStyle = FontStyleExt.Bold,
            ForegroundColor = TableHeaderColor,
        };

        Table table = new TableBuilder(headerFormat)
            .AddColumn("Original name",
                rowsFormat: new CellFormat(
                    foregroundColor: TableKeyColor,
                    alignment: Alignment.Left
                )
            )
            .AddColumn("Original offset",
                rowsFormat: new CellFormat(
                    foregroundColor: TableKeyColor,
                    alignment: Alignment.Left
                )
            )
            .AddColumn("Today's offset",
                rowsFormat: new CellFormat(
                    foregroundColor: TableValueColor,
                    alignment: Alignment.Left
                )
            )
            .AddColumn("Modern ID",
                rowsFormat: new CellFormat(
                    foregroundColor: TableValueColor,
                    alignment: Alignment.Left
                )
            )
            .Build();

        foreach (Tz tz in _tzs)
        {
            TimeSpan modernUtcOffset = tz.ModernTimeZone.GetUtcOffset(DateTimeOffset.UtcNow);

            string originalOffsetStr = tz.TimeZoneProto.OriginalUtcOffset.ToUtcString();
            string modernOffsetStr = modernUtcOffset.ToUtcString();

            table.AddRow(tz.TimeZoneProto.OriginalTzId.Value, originalOffsetStr,  modernOffsetStr, tz.TimeZoneProto.ModernTzId);
        }

        table.Config = TableConfig.Unicode();

        return $"{table}";
    }

    private static TimeSpan ParseOriginalUtcOffset(RomString offsetStr)
    {
        s8 relativeOffset = s8.Parse(offsetStr.Value);
        s8 utcOffset = (s8)(relativeOffset - 5);
        return TimeSpan.FromHours(utcOffset);
    }

    private static string GetModernTzId(RomString originalId)
    {
        return originalId.Value.Trim() switch
        {
            "Anchorage" => "America/Anchorage",
            "Atlantic" => "America/Puerto_Rico",
            "Baghdad" => "Asia/Baghdad",
            "Bangui" => "Africa/Bangui",
            "Barnaul" => "Asia/Barnaul",
            "Beijing" => "Asia/Shanghai",
            "Berlin" => "Europe/Berlin",
            "Cairo" => "Africa/Cairo",
            "Chicago" => "America/Chicago",
            "Chita" => "Asia/Chita",
            "Darwin" => "Australia/Darwin",
            "Denver" => "America/Denver",
            "Godthab" => "America/Scoresbysund",
            "Helsinki" => "Europe/Helsinki",
            "HongKong" => "Asia/Hong_Kong",
            "Honolulu" => "Pacific/Honolulu",
            "Istanbul" => "Europe/Istanbul",
            "Izhevsk" => "Asia/Yekaterinburg",
            "LasVegas" => "America/Los_Angeles",
            "Lima" => "America/Lima",
            "London" => "Europe/London",
            "LosAngeles" => "America/Los_Angeles",
            "Madrid" => "Europe/Madrid",
            "Miami" => "America/New_York",
            "Moscow" => "Europe/Moscow",
            "NewYork" => "America/New_York",
            "Orenberg" => "Asia/Yekaterinburg",
            "Oslo" => "Europe/Oslo",
            "Paris" => "Europe/Paris",
            "Perth" => "Australia/Perth",
            "Reykjavik" => "Atlantic/Reykjavik",
            "Rio" => "America/Rio_Branco",
            "Rome" => "Europe/Rome",
            "Santiago" => "America/Santiago",
            "Stockholm" => "Europe/Stockholm",
            "Sydney" => "Australia/Sydney",
            "Tokyo" => "Asia/Tokyo",
            "Vancouver" => "America/Vancouver",
            "Vienna" => "Europe/Vienna",
            "Washington" => "America/New_York",
            "Wellington" => "Pacific/Auckland",
            _ => "UTC",
        };
    }

    private class Tz
    {
        public readonly GbcSmxTimeZone TimeZoneProto;
        public readonly TimeZoneInfo ModernTimeZone;

        public Tz(byte tzIndex, RomString offsetStr, RomString originalId)
        {
            string modernTzId = GetModernTzId(originalId);
            ModernTimeZone = TimeZoneInfo.FindSystemTimeZoneById(modernTzId);
            TimeZoneProto = new GbcSmxTimeZone()
            {
                ListIndex = tzIndex,
                OriginalOffsetStr = offsetStr,
                OriginalTzId = originalId,
                ModernTzId = modernTzId,
                OriginalUtcOffset = Duration.FromTimeSpan(ParseOriginalUtcOffset(offsetStr)),
                TodayUtcOffset = Duration.FromTimeSpan(ModernTimeZone.BaseUtcOffset),
            };
        }
    }
}
