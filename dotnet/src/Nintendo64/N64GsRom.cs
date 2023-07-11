using System.Text.RegularExpressions;
using Google.Protobuf;
using Google.Protobuf.Collections;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Cli;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;
using Spectre.Console;

namespace LibreShark.Hammerhead.Nintendo64;

/// <summary>
/// GameShark (USA/CAN), Action Replay (UK/EU), Equalizer (UK/EU), and Game Buster (Germany) for
/// Nintendo 64, made by Datel/InterAct.
///
/// See
/// https://github.com/Jhynjhiruu/gameshark/blob/aeed3cb6478904f9479f56743238d0d0ecfbce78/gameshark.us.yaml
/// https://github.com/Jhynjhiruu/gameshark/blob/aeed3cb6478904f9479f56743238d0d0ecfbce78/src/lzari.c#L619
/// </summary>
public sealed class N64GsRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.Nintendo64;
    private const CodecId ThisCodecId = CodecId.N64GamesharkRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    private static readonly Regex FileNameRegex = new Regex(@"^[\w~.]+$");

    /// <summary>
    /// The GS firmware will silently truncate names beyond this length.
    /// The official PC utils will crash if you try to use longer names.
    /// </summary>
    private const u8 GameNameMaxDisplayLen = 30;

    /// <summary>
    /// The GS firmware will silently truncate names beyond this length.
    /// The official PC utils will crash if you try to use longer names.
    /// </summary>
    private const u8 CheatNameMaxDisplayLen = 30;

    public static N64GsRom Create(string filePath, u8[] rawInput)
    {
        return new N64GsRom(filePath, rawInput);
    }

    private readonly bool _isV3Firmware;
    private readonly bool _isV1GameList;
    private readonly bool _isV3KeyCodeListAddr;

    private readonly u32 _firmwareAddr;
    private readonly u32 _gameListAddr;
    private readonly u32 _keyCodeListAddr;
    private readonly u32 _userPrefsAddr;
    private readonly RomString _headerId;
    private readonly RomString _rawTimestamp;
    private readonly N64GsVersion _version;
    private readonly Code _activeKeyCode;

    private const u32 ProgramCounterAddr = 0x00000008;
    private const u32 ActiveKeyCodeAddr  = 0x00000010;
    private const u32 HeaderIdAddr       = 0x00000020;
    private const u32 BuildTimestampAddr = 0x00000030;

    public override CodecId DefaultCheatOutputCodec => CodecId.N64GamesharkText;

    private N64Data Data => Parsed.N64Data;

    private readonly List<EmbeddedFile> _rootCompressedFiles;
    private readonly List<EmbeddedFile> _shellCompressedFiles;

    private EmbeddedFile? ShellFile => _rootCompressedFiles.FirstOrDefault(file => file.FileName == "shell.bin");

    private N64GsRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, Decrypt(rawInput), ThisConsoleId, ThisCodecId)
    {
        Parsed.N64Data = new N64Data();

        Support.SupportsCheats = true;
        Support.SupportsKeyCodes = true;
        Support.SupportsFirmware = true;
        Support.SupportsFileExtraction = true;
        Support.SupportsFileEncryption = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;

        _headerId = Scribe.Seek(HeaderIdAddr).ReadCStringUntilNull(0x10, false);
        _rawTimestamp = Scribe.Seek(BuildTimestampAddr).ReadPrintableCString(16, true);

        Metadata.Identifiers.Add(_headerId);
        Metadata.Identifiers.Add(_rawTimestamp);

        Support.SupportsFirmwareCompression = DetectCompressed(rawInput);
        Support.IsFirmwareCompressed        = DetectCompressed(rawInput);
        Support.IsFileEncrypted             = DetectEncrypted(rawInput);

        _rootCompressedFiles = ReadRootFiles();
        _shellCompressedFiles = ReadShellFiles();

        _version = ReadVersion();

        Metadata.BrandId = _version.Brand;
        Metadata.BuildDateRaw = _rawTimestamp;
        Metadata.BuildDateIso = _version.DisplayBuildTimestampIso;
        Metadata.DisplayVersion = _version.DisplayNumber;
        Metadata.SortableVersion = _version.Number; // TODO(CheatoBaggins): Account for April/May builds
        Metadata.IsKnownVersion = _version.IsKnown;
        Metadata.LanguageIetfCode = _version.Locale.Name;

        _isV3Firmware        = Scribe.Seek(0x00001000).ReadU32() == 0x00000000;
        _isV1GameList        = Scribe.Seek(0x0002DFF0).ReadU32() == 0x00000000;
        _isV3KeyCodeListAddr = Scribe.Seek(0x0002FBF0).ReadU32() == 0xFFFFFFFF;
        _keyCodeListAddr     = (u32)(_isV3KeyCodeListAddr ? 0x0002FC00 : 0x0002D800);

        Support.SupportsUserPrefs = Scribe.Seek(0x0002FAF0).ReadU32() == 0xFFFFFFFF;
        Support.HasKeyCodes       = Scribe.Seek(_keyCodeListAddr).ReadU32() != 0x00000000;

        _firmwareAddr  = (u32)(_isV3Firmware ? 0x00001080 : 0x00001000);
        _gameListAddr  = (u32)(_isV1GameList ? 0x0002E000 : 0x00030000);
        _userPrefsAddr = Support.SupportsUserPrefs ? 0x0002FB00 : 0xFFFFFFFF;

        Data.KeyCodes.AddRange(ReadKeyCodes());
        _activeKeyCode = ReadActiveKeyCode();

        Support.HasPristineUserPrefs = Support.SupportsUserPrefs &&
                                       Scribe.Seek(_userPrefsAddr).IsPadding();

        Games.AddRange(ReadGames());

        EmbeddedFiles.AddRange(_rootCompressedFiles);
        EmbeddedFiles.AddRange(_shellCompressedFiles);
        EmbeddedImages.AddRange(GetLogoImages(_rootCompressedFiles));
        EmbeddedImages.AddRange(GetTileImages(_rootCompressedFiles));
        EmbeddedImages.AddRange(GetTileImages(_shellCompressedFiles));
    }

    private List<EmbeddedImage> GetLogoImages(List<EmbeddedFile> files)
    {
        var decoder = new N64GsImageDecoder();

        List<EmbeddedFile> paletteFiles = files.Where(file => file.FileName.EndsWith(".pal")).ToList();

        return (
            from paletteFile in paletteFiles
            let imageFile = files.First(
                curFile => curFile.FileName == paletteFile.FileName.Replace(".pal", ".bin")
            )
            let image = decoder.DecodeStartupLogo(
                paletteFile.UncompressedBytes,
                imageFile.UncompressedBytes,
                true,
                new Rgb24(0, 0, 0)
            )
            select new EmbeddedImage(paletteFile.FileName, image)
        ).ToList();
    }

    private List<EmbeddedImage> GetTileImages(List<EmbeddedFile> files)
    {
        var decoder = new N64GsImageDecoder();
        return files
            .Where(file => file.FileName.EndsWith(".tg~"))
            .Select(file =>
                {
                    Image<Rgba32> image = decoder.Decode16BitRgba(file.UncompressedBytes, file.FileName);
                    return new EmbeddedImage(file.FileName, image);
                }
            )
            .ToList();
    }

    private List<EmbeddedFile> ReadRootFiles()
    {
        // TODO(CheatoBaggins): Figure out how to read images from
        // uncompressed ROMs (v2.4 and earlier).
        if (!Support.IsFirmwareCompressed)
        {
            return new List<EmbeddedFile>();
        }
        return ReadAllFiles(Buffer);
    }

    private List<EmbeddedFile> ReadShellFiles()
    {
        // TODO(CheatoBaggins): Figure out how to read images from
        // uncompressed ROMs (v2.4 and earlier).
        if (!Support.IsFirmwareCompressed)
        {
            return new List<EmbeddedFile>();
        }

        if (!ShellFile.HasValue)
        {
            return new List<EmbeddedFile>();
        }

        return ReadAllFiles(ShellFile.Value.UncompressedBytes);
    }

    private List<EmbeddedFile> ReadAllFiles(u8[] fsblob)
    {
        var files = new List<EmbeddedFile>();

        // TODO(CheatoBaggins): Figure out how to read images from
        // uncompressed ROMs (v2.4 and earlier).
        if (!Support.IsFirmwareCompressed || fsblob.Length == 0)
        {
            return files;
        }

        var scribe = new BigEndianScribe(fsblob);
        var decoder = new N64GsLzariEncoder();

        // The first occurrence of each filename is used internally by the
        // GS firmware to look up the location of the embedded file in the
        // fsblob section.
        //
        // The second occurrence of each filename is found inside the fsblob,
        // and marks the start of the embedded file.
        //
        // Each file is stored as a struct containing the byte length of the
        // entire struct (encoded as a big endian u32), the name of the file
        // as a C string, and finally the actual (compressed) contents of
        // the file.
        //
        // shell.bin is always the first file stored in the ROM,
        // and tile1.tg~ is always the first file stored in the shell.
        int[] nameOffsets = fsblob.FindAll("shell.bin");
        if (nameOffsets.Length == 0)
        {
            nameOffsets = fsblob.FindAll("tile1.tg~");
        }
        if (nameOffsets.Length == 0)
        {
            return files;
        }

        int structOffset = nameOffsets[1] - 4;

        scribe.Seek(structOffset);

        while (true)
        {
            if (scribe.EndReached ||
                scribe.Position >= fsblob.Length - 0x10 ||
                scribe.IsPadding())
            {
                break;
            }
            u32 structLen = scribe.ReadU32();

            // Account for the length (4-byte u32) and name (12-byte string) fields.
            u32 dataLen = structLen - 0x10;

            string curFileName = scribe.ReadFixedLengthPrintableCString(12).Value.Trim();
            if (!FileNameRegex.IsMatch(curFileName))
            {
                break;
            }

            u8[] compressedBytes = scribe.ReadBytes(dataLen);
            var file = new EmbeddedFile(curFileName, compressedBytes, decoder.Decode(compressedBytes));
            files.Add(file);
        }

        return files;
    }

    protected override void SanitizeCustomProtoFields(ParsedFile parsed)
    {
        foreach (var kc in parsed.N64Data.KeyCodes)
        {
            kc.CodeName = kc.CodeName.WithoutAddress();
        }
    }

    private List<Game> ReadGames()
    {
        Scribe.Seek(_gameListAddr);
        List<Game> games = new List<Game>();
        u32 gamesCount = Scribe.ReadU32();
        for (u32 gameIdx = 0; gameIdx < gamesCount; gameIdx++)
        {
            games.Add(ReadGame(gameIdx));
        }
        return games;
    }

    private Game ReadGame(u32 gameIdx)
    {
        var game = new Game()
        {
            GameIndex = gameIdx,
            GameName = ReadName(),
        };
        u8 cheatCount = Scribe.ReadU8();
        for (u8 cheatIdx = 0; cheatIdx < cheatCount; cheatIdx++)
        {
            ReadCheat(game, cheatIdx);
        }
        return game;
    }

    private void ReadCheat(Game game, u8 cheatIdx)
    {
        var cheat = new Cheat()
        {
            CheatIndex = cheatIdx,
            CheatName = ReadName(),
        };
        game.Cheats.Add(cheat);
        u8 codeCount = Scribe.ReadU8();
        bool cheatOn = (codeCount & 0x80) > 0;
        codeCount &= 0x7F;
        cheat.IsCheatActive = cheatOn;
        for (u8 codeIdx = 0; codeIdx < codeCount; codeIdx++)
        {
            ReadCode(cheat, codeIdx);
        }
    }

    private void ReadCode(Cheat cheat, u8 codeIdx)
    {
        u8[] bytes = Scribe.ReadBytes(6);
        cheat.Codes.Add(new Code()
        {
            CodeIndex = codeIdx,
            Bytes = ByteString.CopyFrom(bytes),
        });
    }

    private RomString ReadName()
    {
        u32 startPos = Scribe.Position;

        RomString name = Scribe.ReadPrintableCString();

        if (name.Value.Length < 1)
        {
            Console.Error.WriteLine($"WARNING at offset 0x{startPos:X8}: Game and Cheat names should contain at least 1 character.");
            return name;
        }

        name.Value = string.Join("", name.Value.Select(c =>
        {
            return (int)c switch
            {
                0xF6 => "Key",
                0xF7 => "Have ",
                0xF8 => "Lives",
                0xF9 => "Energy",
                0xFA => "Health",
                0xFB => "Activate ",
                0xFC => "Unlimited ",
                0xFD => "Player ",
                0xFE => "Always ",
                0xFF => "Infinite ",
                _ => c.ToString(),
            };
        }));

        return name;
    }

    private Code ReadActiveKeyCode()
    {
        u8[] activeCrcBytes = Scribe.PeekBytesAt(ActiveKeyCodeAddr, 8);
        u8[] activePcBytes = Scribe.PeekBytesAt(ProgramCounterAddr, 4);

        RomString? name = null;
        if (Data.KeyCodes.Count > 0)
        {
            Code? matchingKeyCodeInList = Data.KeyCodes.ToList().Find(kc =>
            {
                u8[] curKeyCodeCrcBytes = kc.Bytes.ToArray()[..8];
                return curKeyCodeCrcBytes.SequenceEqual(activeCrcBytes);
            });
            name = matchingKeyCodeInList?.CodeName;
        }

        var kc = new Code()
        {
            CodeName = name ?? "probably CIC-NUS-6102".ToRomString(),
            IsActiveKeyCode = true,
        };

        u8[] keyCodeBytes = activeCrcBytes.Concat(activePcBytes).ToArray();
        u8 checkDigit = N64GsChecksum.GetCheckDigit(Buffer, keyCodeBytes);
        keyCodeBytes = keyCodeBytes.Concat(new u8[] { checkDigit }).ToArray();
        kc.Bytes = ByteString.CopyFrom(keyCodeBytes);

        return kc;
    }

    private List<Code> ReadKeyCodes()
    {
        u8[] activePrefix = Scribe.PeekBytesAt(ActiveKeyCodeAddr, 8);

        Scribe.Seek(_keyCodeListAddr);
        u8[] listBytes = Scribe.PeekBytes(0xA0);
        u32 maxPos = Scribe.Position + (u32)listBytes.Length;
        s32 keyCodeByteLength = new s32[]
        {
            listBytes.Find("Mario"),
            listBytes.Find("Diddy"),
            listBytes.Find("Yoshi"),
            listBytes.Find("Zelda"),
        }.Min();

        // Valid key codes are either 9 or 13 bytes long.
        if (keyCodeByteLength < 9)
        {
            return new List<Code>();
        }

        List<Code> keyCodes = new();
        while (Scribe.Position <= maxPos)
        {
            u8[] bytes = Scribe.ReadBytes((u32)keyCodeByteLength);
            RomString name = Scribe.ReadPrintableCString(31, true);
            while (Scribe.PeekBytes(1)[0] == 0)
            {
                Scribe.ReadU8();
            }
            bool isActive = bytes.Contains(activePrefix);
            var keyCode = new Code() {
                CodeName = name,
                Bytes = ByteString.CopyFrom(bytes),
                IsActiveKeyCode = isActive,
            };
            keyCodes.Add(keyCode);
        }
        return keyCodes;
    }

    // TODO(CheatoBaggins): Move this method to the N64GsVersion class
    private N64GsVersion ReadVersion()
    {
        RomString? titleVersionNumberStr =
            // Original brands
            ReadMainMenuTitle("N64 GameShark Version ") ??
            ReadMainMenuTitle("GameShark Pro Version ") ??
            ReadMainMenuTitle("N64 Action Replay Version ") ??
            ReadMainMenuTitle("Action Replay Pro Version ") ??
            ReadMainMenuTitle("N64 Equalizer Version ") ??
            ReadMainMenuTitle("N64 Game Buster Version ") ??
            // LibreShark
            ReadMainMenuTitle("LibreShark Version ") ??
            ReadMainMenuTitle("LibreShark Pro Version ") ??
            ReadMainMenuTitle("LibreShark Version ") ??
            ReadMainMenuTitle("LibreShark Pro Version ") ??
            // Unknown
            null;

        if (titleVersionNumberStr != null)
        {
            Metadata.Identifiers.Add(titleVersionNumberStr);
        }

        N64GsVersion? version = N64GsVersion.From(_rawTimestamp.Value, titleVersionNumberStr);
        if (version == null)
        {
            throw new InvalidDataException("Failed to find N64 GameShark ROM version!");
        }

        return version;
    }

    private RomString? ReadMainMenuTitle(string needle)
    {
        s32 titleLength = needle.Length + 5;

        if (IsFirmwareCompressed() && ShellFile.HasValue)
        {
            u8[] shellBytes = ShellFile.Value.UncompressedBytes;
            s32 titleVersionPos = shellBytes.Find(needle);
            if (titleVersionPos == -1)
            {
                return null;
            }

            u8[] titleBytes = shellBytes[titleVersionPos..(titleVersionPos + titleLength)];
            return titleBytes.ToAsciiString().ToRomString();
        }
        else
        {
            u8[] haystack = Buffer[..0x30000];
            s32 titleVersionPos = haystack.Find(needle);
            if (titleVersionPos == -1)
            {
                return null;
            }

            return Scribe.Seek(titleVersionPos).ReadPrintableCString((u32)titleLength, true).Trim();
        }
    }

    public override void ReorderKeyCodes(N64KeyCodeId[] newCicLIst)
    {
        List<Code> oldKeyCodes = Data.KeyCodes.ToList();
        List<N64KeyCodeId> oldCics = oldKeyCodes.Select(GetCicId).ToList();

        var oldCicSet = new HashSet<N64KeyCodeId>(oldCics);
        var newCicSet = new HashSet<N64KeyCodeId>(newCicLIst);

        if (!newCicSet.SetEquals(oldCicSet))
        {
            string oldKeyCodeList = string.Join(", ", oldCicSet);
            string newKeyCodeList = string.Join(", ", newCicSet);
            Printer.PrintError(
                $"This ROM requires [{oldKeyCodeList}] key codes, " +
                $"but got [{newKeyCodeList}].");
            return;
        }

        var newIdx = 0;
        var newKeyCodes = newCicLIst.Select(cic =>
        {
            var oldIdx = oldCics.IndexOf(cic);
            var oldKC = oldKeyCodes[oldIdx];
            var newKC = new Code(oldKC)
            {
                CodeIndex = (u32)newIdx,
                // Reset active key code to the default (first one in the list).
                IsActiveKeyCode = newIdx == 0,
            };
            newIdx++;
            return newKC;
        }).ToList();

        Data.KeyCodes.Clear();
        Data.KeyCodes.AddRange(newKeyCodes);
    }

    private static N64KeyCodeId GetCicId(Code kc)
    {
        string kcName = kc.CodeName.Value;
        if (kcName.StartsWith("Diddy"))
            return N64KeyCodeId.Diddy;
        if (kcName.StartsWith("Yoshi"))
            return N64KeyCodeId.Yoshi;
        if (kcName.StartsWith("Zelda"))
            return N64KeyCodeId.Zelda;
        else
            return N64KeyCodeId.Mario;
    }

    private void WriteName(string str)
    {
        str = str
                .Replace("Key", "\xF6")
                .Replace("Have ", "\xF7")
                .Replace("Lives", "\xF8")
                .Replace("Energy", "\xF9")
                .Replace("Health", "\xFA")
                .Replace("Activate ", "\xFB")
                .Replace("Unlimited ", "\xFC")
                .Replace("Player ", "\xFD")
                .Replace("Always ", "\xFE")
                .Replace("Infinite ", "\xFF")
            ;
        Scribe.WriteCString(str, 30);
    }

    public override ICodec WriteChangesToBuffer()
    {
        // Recalculate CRCs and write key codes to list
        // TODO(CheatoBaggins): What about v2.x ROMs with 9-byte key codes?
        Scribe.Seek(_keyCodeListAddr);
        foreach (Code kc in Data.KeyCodes)
        {
            u8[] bytes = N64GsChecksum.ComputeKeyCode(Buffer, GetCicId(kc));

            kc.Bytes = ByteString.CopyFrom(bytes);
            kc.Formatted = kc.Bytes.ToCodeString(ThisConsoleId);

            u32 startAddr = Scribe.Position;
            Scribe.WriteBytes(bytes);
            Scribe.WriteCString(kc.CodeName);

            // Each key code entry is padded to 0x2C bytes long.
            while (Scribe.Position < startAddr + 0x2C)
            {
                Scribe.WriteU8(0);
            }
        }

        // TODO(CheatoBaggins): What about v2.x ROMs with 9-byte key codes?
        Code activeKC = Data.KeyCodes.First(kc => kc.IsActiveKeyCode);
        u8[] kcBytes = activeKC.Bytes.ToByteArray();
        u8[] checksum = kcBytes[..8];
        u8[] entrypoint = kcBytes[8..12];
        Scribe.Seek(ActiveKeyCodeAddr).WriteBytes(checksum);
        Scribe.Seek(ProgramCounterAddr).WriteBytes(entrypoint);

        Scribe.Seek(_gameListAddr);
        Scribe.WriteU32((u32)Games.Count);
        foreach (Game game in Games)
        {
            WriteName(game.GameName.Value);
            Scribe.WriteU8((u8)game.Cheats.Count);
            foreach (Cheat cheat in game.Cheats)
            {
                WriteName(cheat.CheatName.Value);
                u8 codeCount = (u8)cheat.Codes.Count;
                if (cheat.IsCheatActive)
                {
                    codeCount |= 0x80;
                }
                Scribe.WriteU8(codeCount);
                foreach (Code code in cheat.Codes)
                {
                    Scribe.WriteBytes(code.Bytes);
                }
            }
        }

        u8 pad = Buffer.Last();
        while (!Scribe.EndReached)
        {
            Scribe.WriteU8(pad);
        }

        return this;
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        return is256KiB && (DetectDecrypted(bytes) || DetectEncrypted(bytes));
    }

    public static bool Is(ICodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }

    private static bool DetectDecrypted(u8[] bytes)
    {
        u8[] first4Bytes = bytes[..4];
        bool isN64 = first4Bytes.SequenceEqual(new u8[] { 0x80, 0x37, 0x12, 0x40 }) ||
                     first4Bytes.SequenceEqual(new u8[] { 0x80, 0x37, 0x12, 0x00 });
        const string v1Or2Header = "(C) DATEL D&D ";
        const string v3ProHeader = "(C) MUSHROOM &";
        const string libreShark1 = "(C) Jhynjhiruu"; // First few builds
        const string libreShark2 = "(C) LibreShark"; // All later builds
        return isN64 && (
            bytes.Contains(v1Or2Header) ||
            bytes.Contains(v3ProHeader) ||
            bytes.Contains(libreShark1) ||
            bytes.Contains(libreShark2) ||
            false
        );
    }

    private static bool DetectEncrypted(u8[] bytes)
    {
        return bytes[..7].Contains(new u8[] { 0xAE, 0x59, 0x63, 0x54 });
    }

    private static bool DetectCompressed(u8[] bytes)
    {
        // GameShark v2.50 and later use LZARI compression to store parts of
        // the firmware as embedded files. Filename strings are only present
        // in ROM versions that use compression.
        return bytes.Contains("shell.bin");
    }

    public override u8[] Encrypt()
    {
        return N64GsCrypter.Encrypt(Buffer);
    }

    private static AbstractBinaryScribe Decrypt(u8[] input)
    {
        u8[] output = DetectEncrypted(input)
            ? N64GsCrypter.Decrypt(input)
            : input.ToArray();
        return new BigEndianScribe(output);
    }

    public override void PrintCustomHeader(ICliPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading("Addresses");
        PrintAddressTable(printer);

        printer.PrintHeading("Key codes");
        string hexString = _activeKeyCode.Bytes.ToHexString(" ");
        string nameStr = _activeKeyCode.CodeName.Value;
        printer.PrintN64ActiveKeyCode(_activeKeyCode);
        Console.WriteLine();

        if (Support is { SupportsKeyCodes: true, HasKeyCodes: true })
        {
            PrintKeyCodesTable(printer);
        }
        else
        {
            printer.PrintHint("This firmware version does not support additional key codes.");
        }
    }

    private void PrintAddressTable(ICliPrinter printer)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Section"))
                .AddColumn(printer.HeaderCell("Address"))
            ;

        string firmwareAddr = $"0x{_firmwareAddr:X8}";
        string gameListAddr = $"0x{_gameListAddr:X8}";
        string keyCodeListAddr = Support.SupportsKeyCodes ? $"0x{_keyCodeListAddr:X8}" : "Not supported";
        string userPrefsAddr = Support.SupportsUserPrefs ? $"0x{_userPrefsAddr:X8}" : "Not supported";

        table.AddRow("Firmware", firmwareAddr);
        table.AddRow("User prefs", userPrefsAddr);
        table.AddRow("Key code list", keyCodeListAddr);
        table.AddRow("Game list", gameListAddr);

        printer.PrintTable(table);
    }

    private void PrintKeyCodesTable(ICliPrinter printer)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Games (CIC chip)"))
                .AddColumn(printer.HeaderCell("Key code"))
            ;

        foreach (Code keyCode in Data.KeyCodes)
        {
            string keyCodeName = printer.FormatN64KeyCodeName(keyCode);
            string hexString = printer.FormatN64KeyCodeBytes(keyCode, _activeKeyCode);
            table.AddRow(
                keyCode.IsActiveKeyCode
                    ? $"> {keyCodeName}" + " [ACTIVE]".EscapeMarkup()
                    : $"  {keyCodeName}",
                hexString
            );
        }

        printer.PrintTable(table);
    }
}
