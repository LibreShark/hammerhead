using System.Text.RegularExpressions;
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

    public GbcSharkMxRom(string filePath, u8[] rawInput)
        : base(filePath, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
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

    private List<Tz> _tzs = new();

    private class Tz
    {
        public readonly u8 TzIndex;
        public readonly RomString UtcOffsetStr;
        public readonly RomString TzName;

        public Tz(byte tzIndex, RomString utcOffsetStr, RomString tzName)
        {
            TzIndex = tzIndex;
            UtcOffsetStr = utcOffsetStr;
            TzName = tzName;
        }
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

    protected override void PrintCustomHeader()
    {
        Console.WriteLine($"Time zones ({_tzs.Count}):");
        foreach (Tz tz in _tzs)
        {
            string offsetStr;
            s8 offsetNum = s8.Parse(tz.UtcOffsetStr.Value);
            if (offsetNum == 0)
            {
                offsetStr = "+0";
            }
            else if(offsetNum > 0)
            {
                offsetStr = $"+{offsetNum}";
            }
            else
            {
                offsetStr = offsetNum.ToString();
            }

            offsetStr = offsetStr.PadRight(3);
            Console.WriteLine($"- UTC{offsetStr}: {tz.TzName.Value}");
        }
    }
}
