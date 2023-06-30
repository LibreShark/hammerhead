using System.Text.RegularExpressions;
using BetterConsoles.Tables;
using Google.Protobuf.WellKnownTypes;
using LibreShark.Hammerhead.IO;

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
/// Shark MX email client for Game Boy Color and Game Boy Pocket,
/// made by Datel/InterAct.
/// </summary>
public sealed class GbcSharkMxRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyColor;
    private const CodecId ThisCodecId = CodecId.GbcSharkMxRom;

    private const u32 ContactsAddr  = 0x0000C060;
    private const u32 RegCodeAddr   = 0x00020000;
    private const u32 SecretPinAddr = 0x0002007C;
    private const u32 MessagesAddr  = 0x00021000;

    private readonly List<GbcSmxTimeZone> _tzs = new();
    private readonly List<GbcSmxContact> _contacts = new();
    private readonly List<GbcSmxMessage> _messages = new();

    private RomString _regCodeCopy1 = EmptyRomStr();
    private RomString _regCodeCopy2 = EmptyRomStr();
    private RomString _secretPin = EmptyRomStr();

    public override CodecId DefaultCheatOutputCodec => CodecId.UnsupportedCodecId;

    public GbcSharkMxRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsFirmware = true;
        Support.SupportsUserPrefs = true;
        Support.SupportsSmxMessages = true;

        Support.HasFirmware = true;

        Metadata.BrandId = BrandId.SharkMx;

        ParseRegistrationCode();
        ParseSecretPin();
        ParseVersion();
        ParseTimeZones();
        ParseContacts();
        ParseMessages();
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
                    Metadata.Identifiers.Add(str);
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
        _regCodeCopy1 = Scribe.Seek(RegCodeAddr).ReadCStringUntilNull(16, false);
        _regCodeCopy2 = Scribe.Seek(RegCodeAddr + 16).ReadCStringUntilNull(16, false);

        if (_regCodeCopy1.Value.Length > 0)
        {
            Metadata.Identifiers.Add(_regCodeCopy1);
            Support.HasDirtyUserPrefs = true;
        }
        if (_regCodeCopy2.Value.Length > 0)
        {
            Metadata.Identifiers.Add(_regCodeCopy2);
            Support.HasDirtyUserPrefs = true;
        }
    }

    private void ParseSecretPin()
    {
        Scribe.Seek(SecretPinAddr);
        _secretPin = Scribe.ReadPrintableCString();

        if (_secretPin.Value.Length > 0)
        {
            Metadata.Identifiers.Add(_secretPin);
            Support.HasDirtyUserPrefs = true;
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
            _contacts.Add(ReadNextContact(entryNumStr));
        }

        // There are 50 numbered message contact entries, followed by a single
        // unnumbered contact entry.
        _contacts.Add(ReadNextContact(EmptyRomStr()));
    }

    private RomString ReadNextContactEntryNum()
    {
        RomString entryNumStr;
        byte[] peekBytes = Scribe.PeekBytes(2);

        // There are 50 numbered message contact entries, followed by a single
        // unnumbered contact entry.
        if (peekBytes[0] == 'M' && peekBytes[1] == 'e')
        {
            return EmptyRomStr();
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
            RomString utcOffset = Scribe.ReadCStringUntilNull(3, false).Readable();
            if (!utcOffset.Value.All(IsTzChar))
            {
                break;
            }

            u8[] unknownBytes = Scribe.ReadBytes(2);
            RomString tzName = Scribe.ReadCStringUntilNull(10, true).Readable();
            GbcSmxTimeZone tz = ParseTimeZone(tzIdx, utcOffset, tzName);
            _tzs.Add(tz);
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

            _messages.Add(new GbcSmxMessage()
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

    public override AbstractCodec WriteChangesToBuffer()
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

    public static bool Is(AbstractCodec codec)
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

    public override void PrintCustomHeader(TerminalPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading("Registration");
        Console.WriteLine(BuildRegistrationTable(printer, @params));
    }

    public override void PrintCustomBody(TerminalPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading($"Time zones ({_tzs.Count})");
        Console.WriteLine(BuildTimeZoneTable(printer, @params));
        printer.PrintHeading($"Contacts ({_contacts.Count})");
        Console.WriteLine(BuildContactsTable(printer, @params));
        printer.PrintHeading($"Messages ({_messages.Count})");
        Console.WriteLine(BuildMessagesTable(printer, @params));
    }

    private string BuildTimeZoneTable(TerminalPrinter printer, InfoCmdParams @params)
    {
        Table table = printer.BuildTable(builder =>
        {
            builder
                .AddColumn("Original name", rowsFormat: printer.KeyCell())
                .AddColumn("Original offset", rowsFormat: printer.KeyCell())
                .AddColumn("Today's offset", rowsFormat: printer.ValueCell())
                .AddColumn("Modern ID", rowsFormat: printer.ValueCell())
                ;
        });

        foreach (GbcSmxTimeZone tz in _tzs)
        {
            TimeSpan modernUtcOffset = GetModernTz(tz).GetUtcOffset(DateTimeOffset.UtcNow);

            string originalOffsetStr = tz.OriginalUtcOffset.ToUtcOffsetString();
            string modernOffsetStr = modernUtcOffset.ToUtcOffsetString();

            table.AddRow(tz.OriginalTzId.Value, originalOffsetStr,  modernOffsetStr, tz.ModernTzId);
        }

        return $"{table}";
    }

    private string BuildRegistrationTable(TerminalPrinter printer, InfoCmdParams @params)
    {
        Table table = printer.BuildTable(builder =>
        {
            builder
                .AddColumn("Key", rowsFormat: printer.KeyCell())
                .AddColumn("Value", rowsFormat: printer.ValueCell())
                ;
        });

        table.AddRow("Reg code copy #1", _regCodeCopy1.Value);
        table.AddRow("Reg code copy #2", _regCodeCopy2.Value);
        table.AddRow("Secret PIN", _secretPin.Value);

        return $"{table}";
    }

    private string BuildContactsTable(TerminalPrinter printer, InfoCmdParams @params)
    {
        Table table = printer.BuildTable(builder =>
        {
            builder
                .AddColumn("#", rowsFormat: printer.KeyCell())
                .AddColumn("Name", rowsFormat: printer.ValueCell())
                .AddColumn("Email address", rowsFormat: printer.ValueCell())
                .AddColumn("Unknown field #1", rowsFormat: printer.ValueCell())
                .AddColumn("Unknown field #2", rowsFormat: printer.ValueCell())
                .AddColumn("Phone number", rowsFormat: printer.ValueCell())
                .AddColumn("Street address", rowsFormat: printer.ValueCell())
                ;
        });

        foreach (GbcSmxContact contact in _contacts)
        {
            table.AddRow(
                contact.EntryNumber.Value,
                contact.PersonName.Value,
                contact.EmailAddress.Value,
                contact.UnknownField1.Value,
                contact.UnknownField2.Value,
                contact.PhoneNumber.Value,
                contact.StreetAddress.Value
            );
        }

        return $"{table}";
    }

    private string BuildMessagesTable(TerminalPrinter printer, InfoCmdParams @params)
    {
        Table table = printer.BuildTable(builder =>
        {
            builder
                .AddColumn("Subject", rowsFormat: printer.ValueCell())
                .AddColumn("Recipient", rowsFormat: printer.ValueCell())
                .AddColumn("Unknown field #1", rowsFormat: printer.ValueCell())
                .AddColumn("Raw date", rowsFormat: printer.ValueCell())
                .AddColumn("ISO date", rowsFormat: printer.ValueCell())
                .AddColumn("Message", rowsFormat: printer.ValueCell())
                .AddColumn("Unknown field #2", rowsFormat: printer.ValueCell())
                ;
        });

        foreach (GbcSmxMessage message in _messages)
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
