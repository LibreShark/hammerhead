using System.Text.RegularExpressions;
using Google.Protobuf.WellKnownTypes;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Cli;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;
using Spectre.Console;

namespace LibreShark.Hammerhead.GameBoyColor;

/// <summary>
/// Shark MX email client for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcSharkMxRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyColor;
    private const CodecId ThisCodecId = CodecId.GbcSharkMxRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static GbcSharkMxRom Create(string filePath, u8[] rawInput)
    {
        return new GbcSharkMxRom(filePath, rawInput);
    }

    private const u32 ContactsAddr  = 0x0000C060;
    private const u32 RegCodeAddr   = 0x00020000;
    private const u32 SecretPinAddr = 0x0002007C;
    private const u32 MessagesAddr  = 0x00021000;

    private static readonly string[] KnownDisplayVersions =
    {
        "v1.02 (US)",
    };

    public override CodecId DefaultCheatOutputCodec => CodecId.UnsupportedCodecId;

    private GbcSmxData Data => Parsed.GbcSmxData;

    private GbcSharkMxRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Parsed.GbcSmxData = new GbcSmxData();

        Support.SupportsFirmware = true;
        Support.SupportsUserPrefs = true;
        Support.SupportsSmxMessages = true;

        Support.HasFirmware = true;

        // Assume pristine settings until we start parsing below.
        // If any non-default settings are found, this flag will be flipped
        // to false.
        Support.HasPristineUserPrefs = true;

        Metadata.BrandId = BrandId.SharkMx;

        ParseRegistrationCode();
        ParseSecretPin();
        ParseVersion();
        ParseTimeZones();
        ParseContacts();
        ParseMessages();

        Metadata.IsKnownVersion = KnownDisplayVersions.Contains(Metadata.DisplayVersion);
    }

    protected override void SanitizeCustomProtoFields(ParsedFile parsed)
    {
        foreach (GbcSmxTimeZone tz in parsed.GbcSmxData.Timezones)
        {
            tz.OriginalTzId = tz.OriginalTzId.WithoutAddress();
            tz.OriginalOffsetStr = tz.OriginalOffsetStr.WithoutAddress();
        }
        foreach (GbcSmxContact contact in parsed.GbcSmxData.Contacts)
        {
            contact.EntryNumber = contact.EntryNumber.WithoutAddress();
            contact.PersonName = contact.PersonName.WithoutAddress();
            contact.EmailAddress = contact.EmailAddress.WithoutAddress();
            contact.PhoneNumber = contact.PhoneNumber.WithoutAddress();
            contact.StreetAddress = contact.StreetAddress.WithoutAddress();
            contact.UnknownField1 = contact.UnknownField1.WithoutAddress();
            contact.UnknownField2 = contact.UnknownField2.WithoutAddress();
        }
        foreach (GbcSmxMessage message in parsed.GbcSmxData.Messages)
        {
            message.RecipientEmail = message.RecipientEmail.WithoutAddress();
            message.Subject = message.Subject.WithoutAddress();
            message.Message = message.Message.WithoutAddress();
            message.RawDate = message.RawDate.WithoutAddress();
            message.UnknownField1 = message.UnknownField1.WithoutAddress();
            message.UnknownField2 = message.UnknownField2.WithoutAddress();
        }
    }

    private void ParseVersion()
    {
        s32 welcomeAddr = Scribe.Find("Welcome to");
        s32 manufacturerAddr = Scribe.Find("Shark MX");
        s32 main3GbiAddr = Scribe.Find("MAIN3.GBI");

        if (welcomeAddr > -1)
        {
            RomString welcomeStr = Scribe.Seek(welcomeAddr).ReadCStringUntilNull().Readable();
            Metadata.Identifiers.Add(welcomeStr);

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

        if (manufacturerAddr > -1)
        {
            RomString manufacturerStr = Scribe.Seek(manufacturerAddr).ReadCStringUntilNull().Readable();
            Metadata.Identifiers.Add(manufacturerStr);
        }

        if (main3GbiAddr > -1)
        {
            Scribe.Seek(main3GbiAddr);
            while (!Scribe.IsPadding())
            {
                RomString str = Scribe.ReadPrintableCString();
                if (str.Value.Length > 2)
                {
                    str.Value = Regex.Replace(str.Value, "[^A-Z0-9.]+", "");
                    Metadata.FileNameRefs.Add(str);
                }
                while (!Scribe.IsPadding() && !Scribe.IsPrintableChar())
                {
                    string pos = $"0x{Scribe.Position:X8}";
                    char c = (char)Buffer[Scribe.Position];
                    Scribe.Next();
                }
            }
        }
    }

    private void ParseRegistrationCode()
    {
        Data.RegCode1 = Scribe.Seek(RegCodeAddr).ReadCStringUntilNull(16, false);
        Data.RegCode2 = Scribe.Seek(RegCodeAddr + 16).ReadCStringUntilNull(16, false);

        if (Data.RegCode1.Value.Length > 0)
        {
            Data.RegCode1 = Data.RegCode1;
            Metadata.Identifiers.Add(Data.RegCode1);
            Support.HasPristineUserPrefs = false;
        }
        if (Data.RegCode2.Value.Length > 0)
        {
            Data.RegCode2 = Data.RegCode2;
            Metadata.Identifiers.Add(Data.RegCode2);
            Support.HasPristineUserPrefs = false;
        }
    }

    private void ParseSecretPin()
    {
        Scribe.Seek(SecretPinAddr);
        Data.SecretPin = Scribe.ReadPrintableCString();

        if (Data.SecretPin.Value.Length > 0)
        {
            Data.SecretPin = Data.SecretPin;
            Metadata.Identifiers.Add(Data.SecretPin);
            Support.HasPristineUserPrefs = false;
        }
    }

    private void ParseContacts()
    {
        Scribe.Seek(ContactsAddr);
        while (!Scribe.IsPadding())
        {
            RomString entryNumStr = ReadNextContactEntryNum();
            if (entryNumStr.Value.Length == 0)
            {
                break;
            }
            Data.Contacts.Add(ReadNextContact(entryNumStr));
        }

        // There are 50 numbered message contact entries, followed by a single
        // unnumbered contact entry.
        Data.Contacts.Add(ReadNextContact("".ToRomString()));
    }

    private RomString ReadNextContactEntryNum()
    {
        RomString entryNumStr;
        byte[] peekBytes = Scribe.PeekBytes(2);

        // There are 50 numbered message contact entries, followed by a single
        // unnumbered contact entry.
        if (peekBytes[0] == 'M' && peekBytes[1] == 'e')
        {
            return "".ToRomString();
        }
        else
        {
            entryNumStr = Scribe.ReadCStringUntilNull(2, false);
        }

        // For some reason, the first entry has a non-'0' first byte.
        if (peekBytes[0] == 0xB0)
        {
            entryNumStr.Value = "0" + (char)peekBytes[1];
        }

        return entryNumStr;
    }

    private GbcSmxContact ReadNextContact(RomString entryNumStr)
    {
        return new GbcSmxContact()
        {
            // 1-indexed
            EntryNumber = entryNumStr,
            PersonName = Scribe.ReadCStringUntilNull().Trim(),
            EmailAddress = Scribe.ReadCStringUntilNull().Trim(),
            UnknownField1 = Scribe.ReadCStringUntilNull().Trim(),
            UnknownField2 = Scribe.ReadCStringUntilNull().Trim(),
            PhoneNumber = Scribe.ReadCStringUntilNull().Trim(),
            StreetAddress = Scribe.ReadCStringUntilNull().Trim(),
        };
    }

    private void ParseTimeZones()
    {
        s32 tzListPos = Scribe.Seek(0).Find("Anchorage");
        if (tzListPos < 0)
        {
            Console.Error.WriteLine("ERROR: Unable to find time zones!");
            return;
        }

        u32 tzListAddr = (u32)tzListPos - 5;
        Scribe.Seek(tzListAddr);
        u8 tzIdx = 0;
        while (true)
        {
            RomString utcOffset = Scribe.ReadCStringUntilNull(3, false).Readable().Trim();
            if (string.IsNullOrWhiteSpace(utcOffset.Value) || !utcOffset.Value.All(IsTzChar))
            {
                break;
            }

            u8[] unknownBytes = Scribe.ReadBytes(2);
            RomString tzName = Scribe.ReadCStringUntilNull(10, true).Readable().Trim();
            GbcSmxTimeZone tz = ParseTimeZone(tzIdx, utcOffset, tzName);
            Data.Timezones.Add(tz);
            tzIdx++;
        }
    }

    private static bool IsTzChar(char c)
    {
        return (c is ' ' or '+' or '-') ||
               (c is >= '0' and <= '9');
    }

    private void ParseMessages()
    {
        Scribe.Seek(MessagesAddr);
        while (!Scribe.IsPadding())
        {
            RomString subject = Scribe.ReadCStringUntil(0, '\f').Trim();
            Scribe.Skip(1);
            RomString recipientEmail = Scribe.ReadCStringUntil(0, '\f').Trim();
            Scribe.Skip(1);
            RomString unknownField1 = Scribe.ReadCStringUntil(0, '\f').Trim();
            Scribe.Skip(1);
            RomString rawDate = Scribe.ReadCStringUntil(0, '\f').Trim();
            Scribe.Skip(1);
            RomString message = Scribe.ReadCStringUntil(0, '\x04').Trim();
            Scribe.Skip(1);
            RomString unknownField2 = Scribe.ReadCStringUntilNull().Trim();

            if (!DateTime.TryParse(rawDate.Value, out DateTime dateTime) ||
                dateTime.Year < 1990)
            {
                // Corrupt ROM dump
                return;
            }

            Data.Messages.Add(new GbcSmxMessage()
            {
                Subject = subject,
                RecipientEmail = recipientEmail,
                UnknownField1 = unknownField1,
                RawDate = rawDate,
                IsoDate = dateTime.ToIsoString(),
                Message = message,
                UnknownField2 = unknownField2,
            });

            Support.HasSmxMessages = true;
        }
    }

    public override ICodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        return is256KiB && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        // TODO(CheatoBaggins): Determine whether these strings are present in all ROMs
        // bytes.Contains("Shark MX")
        // bytes.Contains("Datel Design LTD")
        return bytes.Contains("GBMail");
    }

    public static bool Is(ICodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }

    private static AbstractBinaryScribe MakeScribe(u8[] rawInput)
    {
        return new LittleEndianScribe(rawInput.ToArray());
    }

    public override void PrintCustomHeader(ICliPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading("Registration");
        PrintRegistrationTable(printer, @params);
    }

    public override void PrintCustomBody(ICliPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading($"Time zones ({Data.Timezones.Count})");
        PrintTimeZoneTable(printer, @params);

        printer.PrintHeading($"Contacts ({Data.Contacts.Count})");
        PrintContactsTable(printer, @params);

        printer.PrintHeading($"Messages ({Data.Messages.Count})");
        PrintMessagesTable(printer, @params);
    }

    private void PrintTimeZoneTable(ICliPrinter printer, InfoCmdParams @params)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Original name"))
                .AddColumn(printer.HeaderCell("Original offset"))
                .AddColumn(printer.HeaderCell("Today's offset"))
                .AddColumn(printer.HeaderCell("Modern ID"))
            ;

        foreach (GbcSmxTimeZone tz in Data.Timezones)
        {
            TimeSpan modernUtcOffset = GetModernTz(tz).GetUtcOffset(DateTimeOffset.UtcNow);

            string originalOffsetStr = tz.OriginalUtcOffset.ToUtcOffsetString();
            string modernOffsetStr = modernUtcOffset.ToUtcOffsetString();

            table.AddRow(
                tz.OriginalTzId.Value,
                originalOffsetStr,
                modernOffsetStr,
                tz.ModernTzId
            );
        }

        printer.PrintTable(table);
    }

    private void PrintRegistrationTable(ICliPrinter printer, InfoCmdParams @params)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Key"))
                .AddColumn(printer.HeaderCell("Value"))
            ;

        table.AddRow("Reg code, copy #1", Data.RegCode1.Value.EscapeMarkup());
        table.AddRow("Reg code, copy #2", Data.RegCode2.Value.EscapeMarkup());
        table.AddRow("Secret PIN", Data.SecretPin.Value.EscapeMarkup());

        printer.PrintTable(table);
    }

    private void PrintContactsTable(ICliPrinter printer, InfoCmdParams @params)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("#"))
                .AddColumn(printer.HeaderCell("Name"))
                .AddColumn(printer.HeaderCell("Email address"))
                .AddColumn(printer.HeaderCell("Unknown field #1"))
                .AddColumn(printer.HeaderCell("Unknown field #2"))
                .AddColumn(printer.HeaderCell("Phone number"))
                .AddColumn(printer.HeaderCell("Street address"))
                ;

        foreach (GbcSmxContact contact in Data.Contacts)
        {
            string entryNumber = printer.Dim(contact.EntryNumber.Value);
            string personName = contact.PersonName.Value;
            string emailAddress = contact.EmailAddress.Value;
            if (Regex.IsMatch(personName, "^Mem(?:[0-9]{2})?$"))
            {
                personName = printer.Dim(personName);
            }
            if (Regex.IsMatch(emailAddress, "^<Empty>$"))
            {
                emailAddress = printer.Dim(emailAddress);
            }

            table.AddRow(
                entryNumber.EscapeMarkup(),
                personName.EscapeMarkup(),
                emailAddress.EscapeMarkup(),
                contact.UnknownField1.Value.EscapeMarkup(),
                contact.UnknownField2.Value.EscapeMarkup(),
                contact.PhoneNumber.Value.EscapeMarkup(),
                contact.StreetAddress.Value.EscapeMarkup()
            );
        }

        printer.PrintTable(table);
    }

    private void PrintMessagesTable(ICliPrinter printer, InfoCmdParams @params)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Subject"))
                .AddColumn(printer.HeaderCell("Recipient"))
                .AddColumn(printer.HeaderCell("Unknown field #1"))
                .AddColumn(printer.HeaderCell("Raw date"))
                .AddColumn(printer.HeaderCell("ISO date"))
                .AddColumn(printer.HeaderCell("Message"))
                .AddColumn(printer.HeaderCell("Unknown field #2"))
                ;

        foreach (GbcSmxMessage message in Data.Messages)
        {
            table.AddRow(
                message.Subject.Value,
                message.RecipientEmail.Value,
                message.UnknownField1.Value,
                message.RawDate.Value,
                message.IsoDate,
                message.Message.Value,
                message.UnknownField2.Value
            );
        }

        printer.PrintTable(table);
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

    private static GbcSmxTimeZone ParseTimeZone(byte tzIndex, RomString offsetStr, RomString originalId)
    {
        string modernTzId = GetModernTzId(originalId);
        TimeZoneInfo modernTimeZone = TimeZoneInfo.FindSystemTimeZoneById(modernTzId);
        return new GbcSmxTimeZone()
        {
            ListIndex = tzIndex,
            OriginalOffsetStr = offsetStr,
            OriginalTzId = originalId,
            ModernTzId = modernTzId,
            OriginalUtcOffset = Duration.FromTimeSpan(ParseOriginalUtcOffset(offsetStr)),
            TodayUtcOffset = Duration.FromTimeSpan(modernTimeZone.BaseUtcOffset),
        };
    }

    private static TimeZoneInfo GetModernTz(GbcSmxTimeZone tz)
    {
        return TimeZoneInfo.FindSystemTimeZoneById(tz.ModernTzId);
    }
}
