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

        RomString versionRaw = _reader.ReadCStringAt(0x17, 5);
        RomString languageRaw = _reader.ReadCStringAt(0x1C, 1);
        RomString buildRaw = _reader.ReadCStringAt(0x20);
        RomString countryRaw = _reader.ReadCStringAt(0x28);

        // E.g.:
        // - "1.000E build 1772" -> 10_001_772
        // - "1.000E build 1834" -> 10_001_834
        // - "1.067G build 1930" -> 10_671_930
        // - "1.067E build 2510" -> 10_672_510
        // - "1.067E build 2515" -> 10_672_515
        Metadata.SortableVersion = f64.Parse(versionRaw.Value) * 10000000 + s32.Parse(buildRaw.Value);
        Metadata.DisplayVersion = $"v{versionRaw.Value}{languageRaw.Value} build {buildRaw.Value} ({countryRaw.Value})";
        Metadata.LanguageIetfCode = GetIetfCode(languageRaw, countryRaw);

        RomString firstLine = _reader.ReadCStringAt(0x0);
        Metadata.Identifiers.Add(firstLine);
        Metadata.Identifiers.Add(versionRaw);
        Metadata.Identifiers.Add(languageRaw);
        Metadata.Identifiers.Add(buildRaw);
        Metadata.Identifiers.Add(countryRaw);
        RomString fcd = _reader.ReadCStringAt(0x40);
        RomString greetz = _reader.ReadCStringAt(0x800);
        RomString develop = _reader.ReadCStringAt(0x8A0);
        RomString peeps = _reader.ReadCStringAt(0x900);
        RomString link = _reader.ReadCStringAt(0x940);
        Metadata.Identifiers.Add(fcd);
        Metadata.Identifiers.Add(greetz);
        Metadata.Identifiers.Add(develop);
        Metadata.Identifiers.Add(peeps);
        Metadata.Identifiers.Add(link);

        ReadBuildDate(out RomString buildDateRaw, out string buildDateIso, out RomString wayneStr);
        Metadata.BuildDateIso = buildDateIso;
        Metadata.Identifiers.Add(wayneStr);
        Metadata.Identifiers.Add(buildDateRaw);

        _reader.Seek(GameListAddr);
        bool stop = false;
        while (!stop && !_reader.IsSectionPadding())
        {
            RomString gameName = _reader.ReadCString();
            u8 cheatCount = _reader.ReadUByte();

            Game game = new(gameName.Value);

            for (u16 cheatIdx = 0; !stop && cheatIdx < cheatCount; cheatIdx++)
            {
                RomString cheatName = _reader.ReadCString();
                u8 codeCount = _reader.ReadUByte();

                if (cheatName.Value.Length == 0)
                {
                    Console.WriteLine($"{cheatName.Addr.ToDisplayString()}: empty cheat name!");
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
                        if (codeStrNew1 != codeStrNew2)
                        {
                            Console.WriteLine("-------------------");
                            Console.WriteLine($"{cheatName.Value} ({cheatName.Addr})");
                            Console.WriteLine("- encrypted: " + codeStrOld);
                            Console.WriteLine("- method 1:  " + codeStrNew1);
                            Console.WriteLine("- method 2:  " + codeStrNew2);
                            Console.WriteLine("-------------------");
                        }
                        codeBytes = DecryptCodeMethod2(codeBytes);
                        // if (codeStrNew1 == "A0B92EB4 AD87")
                        // {
                        //     Console.WriteLine();
                        // }
                    }

                    BigEndianReader codeReader = new BigEndianReader(codeBytes);
                    u32 addressNum = codeReader.ReadUInt32();
                    u16 valueNum = codeReader.ReadUInt16();
                    cheat.AddCode(addressNum, valueNum);
                }

                game.AddCheat(cheat);
            }

            Games.Add(game);
        }
    }

    private static bool IsCodeEncrypted(byte[] code)
    {
        byte b0 = code[0];
        bool isDecrypted = b0 is
                // Based on https://doc.kodewerx.org/hacking_n64.html#xp_code_types
                0x2A or 0x2C or 0x3C or 0x3F or 0x50 or
                0x80 or 0x81 or 0x82 or 0x83 or 0x85 or
                0x88 or 0x89 or 0x8B or
                0xA0 or 0xA1 or 0xA3 or
                0xB3 or 0xB4 or
                0xD0 or 0xD1 or 0xD2 or
                0xF0 or 0xF1
            ;
        return !isDecrypted;
    }

    // https://doc.kodewerx.org/hacking_n64.html#xp_encryption
    private byte[] EncryptCode(byte[] code)
    {
        return new byte[] {};
    }

    // https://doc.kodewerx.org/hacking_n64.html#xp_encryption
    private byte[] DecryptCodeMethod1(byte[] code)
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
    private byte[] DecryptCodeMethod2(byte[] code)
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

/*
Encryption Algorithm
A0A1A2A3 D0D1
A0 = (a0 XOR 0x68)
A1 = (a1 XOR 0x81) - 0x2B
A2 = (a2 XOR 0x82) - 0x2B
A3 = (a3 XOR 0x83) - 0x2B
D0 = (d0 XOR 0x84) - 0x2B
D1 = (d1 XOR 0x85) - 0x2B

Alternate:
A0 = (a0 XOR 0x68)
A1 = (a1 XOR 0x01) - 0xAB
A2 = (a2 XOR 0x02) - 0xAB
A3 = (a3 XOR 0x03) - 0xAB
D0 = (d0 XOR 0x04) - 0xAB
D1 = (d1 XOR 0x05) - 0xAB

Decryption Algorithm
A0A1A2A3 D0D1
A0 = (A0 XOR 0x68)
A1 = (A1 + 0x2B) XOR 0x81
A2 = (A2 + 0x2B) XOR 0x82
A3 = (A3 + 0x2B) XOR 0x83
D0 = (D0 + 0x2B) XOR 0x84
D1 = (D1 + 0x2B) XOR 0x85

Alternate Method:
A0 = (A0 XOR 0x68)
A1 = (A1 + 0xAB) XOR 0x01
A2 = (A2 + 0xAB) XOR 0x02
A3 = (A3 + 0xAB) XOR 0x03
D0 = (D0 + 0xAB) XOR 0x04
D1 = (D1 + 0xAB) XOR 0x05




Xploder64 / Xplorer64 Code Types
Type 	Description
RAM Writes
8-Bit
80XXXXXX 00?? 	Writes 1 byte (??) to the specified address (XXXXXX) repeatedly.
16-Bit
81XXXXXX ???? 	Writes 2 bytes (????) to the specified address (XXXXXX) repeatedly.
8-Bit XP Button
88XXXXXX 00?? 	Writes 1 byte (??) to the specified address (XXXXXX) each time the XP Button is pressed.
16-Bit XP Button
89XXXXXX ???? 	Writes 2 bytes (????) to the specified address (XXXXXX) each time the XP Button is pressed.
8-Bit Write Once
F0XXXXXX 00?? 	Writes 1 byte (??) to the uncached address (XXXXXX) only once. These are most often used to disable certain types of protection that some games use to disable cheat devices.
16-Bit Write Once
F1XXXXXX ????
Or
2AXXXXXX ???? 	Writes 2 bytes (????) to the uncached address (XXXXXX) only once. These are most often used to disable certain types of protection that some games use to disable cheat devices.
Conditional Codes
8-Bit Equal To
D0XXXXXX 00??
YYYYYYYY ZZZZ 	If the byte at XXXXXXX is equal to ??, then the code on the next line is executed.
16-Bit Equal To
D1XXXXXX ????
YYYYYYYY ZZZZ 	If the 2 bytes at XXXXXXX are equal to ????, then the code on the next line is executed.
Special Codes
Enabler
3CXXXXXX ???? 	The exact effect of this code type is still a mystery.
 */

    private string GetIetfCode(RomString languageRaw, RomString countryRaw)
    {
        switch (languageRaw.Value)
        {
            case "E":
                return "en-GB";
            case "G":
                return "de-DE";
        }
        // Undetermined (unknown)
        return "und";
    }

    private void ReadBuildDate(out RomString buildDateRaw, out string buildDateIso, out RomString wayneStr)
    {
        u32 waynePos = (u32)Bytes.Find("Wayne Hughes Beckett!");
        wayneStr = _reader.ReadCStringAt(waynePos);
        u32 buildDatePos = waynePos + 0x40;
        buildDateRaw = _reader.ReadCStringAt(buildDatePos);
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

    public override bool FormatSupportsFileScrambling()
    {
        return true;
    }

    public override bool IsFileScrambled()
    {
        return DetectScrambled(InitialBytes.ToArray());
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
        var idBytes = bytes[0x40..0x55];
        var idStr = idBytes.ToAsciiString();
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
