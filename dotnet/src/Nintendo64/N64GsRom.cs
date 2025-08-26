using System.Text.RegularExpressions;
using Google.Protobuf;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Cli;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Spectre.Console;
using Color = SixLabors.ImageSharp.Color;
using HorizontalAlignment = SixLabors.Fonts.HorizontalAlignment;
using VerticalAlignment = SixLabors.Fonts.VerticalAlignment;

namespace LibreShark.Hammerhead.Nintendo64;

/// <summary>
/// GameShark (USA/CAN), Action Replay (UK/EU), Equalizer (UK/EU), and Game Buster (Germany) for
/// Nintendo 64, made by Datel/InterAct.
///
/// See
/// https://github.com/Jhynjhiruu/gameshark/blob/aeed3cb6478904f9479f56743238d0d0ecfbce78/gameshark.us.yaml
/// https://github.com/Jhynjhiruu/gameshark/blob/aeed3cb6478904f9479f56743238d0d0ecfbce78/src/lzari.c#L619
/// </summary>
public sealed partial class N64GsRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.Nintendo64;
    private const CodecId ThisCodecId = CodecId.N64GamesharkRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    [GeneratedRegex(@"^[\w~.]+$")]
    private static partial Regex FileNameRegex();

    public override CodecId DefaultCheatOutputCodec => CodecId.N64GamesharkText;

    private EmbeddedFile? ShellFile => _rootCompressedFiles.FirstOrDefault(file => file.FileName == "shell.bin");

    #region Constants

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

    private const u8 KeyCodeNameMaxLen = 30;

    /// <summary>
    /// The user preferences section of the ROM file stores the
    /// selected game index as a single byte.
    /// </summary>
    private const u8 MaxGameCount = 255;

    private const u32 ProgramCounterAddr = 0x00000008;
    private const u32 ActiveKeyCodeAddr  = 0x00000010;
    private const u32 HeaderIdAddr       = 0x00000020;
    private const u32 BuildTimestampAddr = 0x00000030;

    #endregion

    #region Member vars

    private readonly bool _isV3Firmware;
    private readonly bool _isV1GameList;
    private readonly bool _isV3KeyCodeListAddr;

    private readonly u32 _firmwareAddr;
    private readonly u32 _gameListAddr;
    private readonly u32 _keyCodeListAddr;
    private readonly u32 _userPrefsAddr;
    private readonly RomString _headerId;
    private readonly RomString _rawTimestamp;
    private N64GsVersion _version;
    private readonly Code _activeKeyCode;

    private N64Data? N64Data => Parsed.N64Data;

    private readonly List<EmbeddedFile> _rootCompressedFiles;
    private readonly List<EmbeddedFile> _shellCompressedFiles;

    public Image<Rgba32>? StartupLogo
    {
        get
        {
            EmbeddedFile[] logoFiles = _rootCompressedFiles.Where(IsStartupLogo).ToArray();
            if (logoFiles.Length != 2)
            {
                return null;
            }

            EmbeddedFile? paletteFile = logoFiles.FirstOrDefault(file => file.FileName.EndsWith(".pal"));
            EmbeddedFile? dataFile = logoFiles.FirstOrDefault(file => file.FileName.EndsWith(".bin"));
            if (paletteFile == null || dataFile == null)
            {
                return null;
            }

            var n64ImageEncoder = new N64GsImageEncoder();

            Image<Rgba32> image = n64ImageEncoder.DecodeStartupLogo(paletteFile.UncompressedBytes, dataFile.UncompressedBytes);

            return image;
        }
    }

    public Image<Rgba32>? StartupTile
    {
        get
        {
            EmbeddedFile[] tileFiles = _rootCompressedFiles.Where(IsBackgroundTile).ToArray();
            if (tileFiles.Length != 1)
            {
                return null;
            }

            var n64ImageEncoder = new N64GsImageEncoder();

            EmbeddedFile tileFile = tileFiles[0];
            Image<Rgba32> image = n64ImageEncoder.DecodeTileGraphic(tileFile.UncompressedBytes, tileFile.FileName);

            return image;
        }
    }

    public Image<Rgba32>? StartupScreenComposite =>
        new N64GsImageEncoder().CompositeStartupScreen(StartupTile, StartupLogo);

    private RomString? _mainMenuTitle;

    #endregion

    #region Constructor

    public static N64GsRom Create(string filePath, u8[] rawInput)
    {
        return new N64GsRom(filePath, rawInput);
    }

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

        _headerId = Scribe.Seek(HeaderIdAddr).ReadPrintableCString(0x10, false);
        _rawTimestamp = Scribe.Seek(BuildTimestampAddr).ReadPrintableCString(16, true);

        Metadata.Identifiers.Add(_headerId);
        Metadata.Identifiers.Add(_rawTimestamp);

        Support.SupportsFirmwareCompression = DetectCompressed(rawInput);
        Support.IsFirmwareCompressed        = DetectCompressed(rawInput);
        Support.IsFileEncrypted             = DetectEncrypted(rawInput);

        _rootCompressedFiles = ReadRootFiles();
        _shellCompressedFiles = ReadShellFiles();

        _version = ReadVersionMetadata();

        Metadata.TvStandard = ReadTvStandard();

        _isV3Firmware        = Scribe.Seek(0x00001000).ReadU32() == 0x00000000;
        _isV1GameList        = Scribe.Seek(0x0002DFF0).ReadU32() == 0x00000000;
        _isV3KeyCodeListAddr = Scribe.Seek(0x0002FBF0).ReadU32() == 0xFFFFFFFF;
        _keyCodeListAddr     = (u32)(_isV3KeyCodeListAddr ? 0x0002FC00 : 0x0002D800);

        Support.SupportsUserPrefs = Scribe.Seek(0x0002FAF0).ReadU32() == 0xFFFFFFFF;

        _firmwareAddr  = (u32)(_isV3Firmware ? 0x00001080 : 0x00001000);
        _gameListAddr  = (u32)(_isV1GameList ? 0x0002E000 : 0x00030000);
        _userPrefsAddr = Support.SupportsUserPrefs ? 0x0002FB00 : 0xFFFFFFFF;

        Parsed.N64Data.KeyCodes.AddRange(ReadKeyCodes());
        _activeKeyCode = ReadActiveKeyCode();
        Support.HasKeyCodes = Parsed.N64Data.KeyCodes.Count > 0;

        Support.HasPristineUserPrefs = Support.SupportsUserPrefs &&
                                       Scribe.Seek(_userPrefsAddr).IsPadding();

        Parsed.N64Data.GsUserPrefs = ReadUserPrefs();

        Games.AddRange(ReadGames());

        EmbeddedFiles.AddRange(_rootCompressedFiles);
        EmbeddedFiles.AddRange(_shellCompressedFiles);
        EmbeddedImages.AddRange(GetLogoImages(_rootCompressedFiles));
        EmbeddedImages.AddRange(GetTileImages(_rootCompressedFiles));
        EmbeddedImages.AddRange(GetTileImages(_shellCompressedFiles));
    }

    private N64GsVersion ReadVersionMetadata()
    {
        _version = ReadVersion();

        Metadata.BrandId = _version.Brand;
        Metadata.BuildDateRaw = _rawTimestamp;
        Metadata.BuildDateIso = _version.DisplayBuildTimestampIso;
        Metadata.DisplayVersion = _version.DisplayNumber;
        Metadata.SortableVersion = _version.Number; // TODO(CheatoBaggins): Account for April/May builds
        Metadata.IsKnownVersion = _version.IsKnown;
        Metadata.LanguageIetfCode = _version.Locale.Name;

        return _version;
    }

    #endregion

    #region Reading metadata & user prefs

    // TODO(CheatoBaggins): Move this method to the N64GsVersion class
    private N64GsVersion ReadVersion()
    {
        _mainMenuTitle =
            // Original brands
            ReadMainMenuTitle("N64 GameShark Version ") ??
            ReadMainMenuTitle("GameShark Pro Version ") ??
            ReadMainMenuTitle("N64 Action Replay Version ") ??
            ReadMainMenuTitle("Action Replay Pro Version ") ??
            ReadMainMenuTitle("N64 Equalizer Version ") ??
            ReadMainMenuTitle("N64 Game Buster Version ") ??
            // LibreShark
            ReadMainMenuTitle("N64 LibreShark Pro Version ") ??
            ReadMainMenuTitle("N64 LibreShark Pro v") ??
            ReadMainMenuTitle("N64 LibreShark Version ") ??
            ReadMainMenuTitle("N64 LibreShark v") ??
            ReadMainMenuTitle("LibreShark Version ") ??
            ReadMainMenuTitle("LibreShark v") ??
            ReadMainMenuTitle("LibreShark Pro Version ") ??
            ReadMainMenuTitle("LibreShark Pro v") ??
            // Unknown
            null;

        if (_mainMenuTitle != null)
        {
            Metadata.Identifiers.Add(_mainMenuTitle);
        }

        N64GsVersion? version = N64GsVersion.From(_rawTimestamp.Value, _mainMenuTitle);
        if (version == null)
        {
            throw new InvalidDataException("Failed to find N64 GameShark ROM version!");
        }

        return version;
    }

    private RomString? ReadMainMenuTitle(string needle)
    {
        if (IsFirmwareCompressed() && ShellFile != null)
        {
            u8[] shellBytes = ShellFile.UncompressedBytes;
            s32 titleVersionPos = shellBytes.Find(needle);
            if (titleVersionPos == -1)
            {
                return null;
            }
            return new BigEndianScribe(shellBytes).Seek(titleVersionPos).ReadFixedLengthPrintableCString(28);
        }
        else
        {
            u8[] haystack = Buffer[..0x30000];
            s32 titleVersionPos = haystack.Find(needle);
            if (titleVersionPos == -1)
            {
                return null;
            }
            return Scribe.Seek(titleVersionPos).ReadFixedLengthPrintableCString(28);
        }
    }

    private N64GsUserPrefs? ReadUserPrefs()
    {
        if (!Support.SupportsUserPrefs)
        {
            return null;
        }
        if (Support.HasPristineUserPrefs)
        {
            return MakePristinePrefs();
        }

        Scribe.Seek(_userPrefsAddr);
        // Ignore magic "GT" bytes
        Scribe.Skip(2);
        bool isSoundEnabled = Scribe.ReadBool();
        var bgPattern = Scribe.ReadEnum8<Nn64GsBgPatternId>();
        var bgColor = Scribe.ReadEnum8<Nn64GsBgColorId>();
        u8 selectedGameIndexStartingAt1 = Scribe.ReadU8();
        Scribe.Skip(1);
        bool isBgScrollEnabled = Scribe.ReadBool();
        Scribe.Skip(100);
        bool isMenuScrollEnabled = Scribe.ReadBool();
        return new N64GsUserPrefs()
        {
            SelectedGameIndex = selectedGameIndexStartingAt1 == 0 ? -1 : selectedGameIndexStartingAt1 - 1,
            BgPatternId = bgPattern,
            BgColorId = bgColor,
            IsSoundEnabled = isSoundEnabled,
            IsBgScrollEnabled = isBgScrollEnabled,
            IsMenuScrollEnabled = isMenuScrollEnabled,
        };
    }

    private TvStandardId ReadTvStandard()
    {
        // Detect the machine code generated from this line in main.c:
        //     vi_regs[6] = 525;
        // https://github.com/Jhynjhiruu/gameshark/blob/8d082808b4eb4e7f4de9b1a91d5452bf18a00eb8/src/main.c#L127
        //
        // v2.50 and above ONLY!
        // Earlier firmwares use different (as yet undiscovered) machine code.
        bool isV3RomNtsc = Buffer.Contains("2403020DAC430018".HexToBytes());
        if (isV3RomNtsc)
            return TvStandardId.Ntsc;

        // Detect the machine code generated from this line in main.c:
        //     vi_regs[6] = 625;
        // https://github.com/Jhynjhiruu/gameshark/blob/8d082808b4eb4e7f4de9b1a91d5452bf18a00eb8/src/main.c#L135
        //
        // v2.50 and above ONLY!
        // Earlier firmwares use different (as yet undiscovered) machine code.
        bool isV3RomPal = Buffer.Contains("24030271AC430018".HexToBytes());
        if (isV3RomPal)
            return TvStandardId.Pal;

        return TvStandardId.UnspecifiedTvStandard;
    }

    #endregion

    #region Reading embedded files/images

    private List<EmbeddedImage> GetLogoImages(List<EmbeddedFile> files)
    {
        var decoder = new N64GsImageEncoder();

        List<EmbeddedFile> paletteFiles = files.Where(file => file.FileName.EndsWith(".pal")).ToList();

        return (
            from paletteFile in paletteFiles
            let imageFile = files.First(
                curFile => curFile.FileName == paletteFile.FileName.Replace(".pal", ".bin")
            )
            let image = decoder.DecodeStartupLogo(
                paletteFile.UncompressedBytes,
                imageFile.UncompressedBytes
            )
            select new EmbeddedImage(paletteFile.FileName, image)
        ).ToList();
    }

    private List<EmbeddedImage> GetTileImages(List<EmbeddedFile> files)
    {
        var decoder = new N64GsImageEncoder();
        return files
            .Where(file => file.FileName.EndsWith(".tg~"))
            .Select(file =>
                {
                    Image<Rgba32> image = decoder.DecodeTileGraphic(file.UncompressedBytes, file.FileName);
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

        if (ShellFile == null)
        {
            return new List<EmbeddedFile>();
        }

        return ReadAllFiles(ShellFile.UncompressedBytes);
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

            u32 startPos = scribe.Position;
            u32 structLen = scribe.ReadU32();

            // Account for the length (4-byte u32) and name (12-byte string) fields.
            u32 dataLen = structLen - 0x10;

            string curFileName = scribe.ReadFixedLengthPrintableCString(12).Value.Trim();
            if (!FileNameRegex().IsMatch(curFileName))
            {
                // We've reached the end of the stream of embedded files
                break;
            }

            u8[] compressedBytes = scribe.ReadBytes(dataLen);
            u32 endPos = scribe.Position;

            var file = new EmbeddedFile(
                curFileName,
                compressedBytes,
                decoder.Decode(compressedBytes),
                new RomRange()
                {
                    Name = curFileName,
                    Length = endPos - startPos,
                    StartIndex = startPos,
                    EndIndex = endPos,
                }
            );
            files.Add(file);
        }

        return files;
    }

    protected override void SanitizeCustomProtoFields(ParsedFile parsed)
    {
        if (parsed.N64Data == null)
        {
            return;
        }
        foreach (var kc in parsed.N64Data.KeyCodes)
        {
            kc.CodeName = kc.CodeName.WithoutAddress();
        }
    }

    #endregion

    #region Reading games/cheats/codes

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
        int selectedGameIndex = GetSelectedGameIndexPref();
        var game = new Game()
        {
            GameIndex = gameIdx,
            GameName = ReadName(),
            IsGameActive = gameIdx == selectedGameIndex,
        };
        u8 cheatCount = Scribe.ReadU8();
        for (u8 cheatIdx = 0; cheatIdx < cheatCount; cheatIdx++)
        {
            ReadCheat(game, cheatIdx);
        }
        return game;
    }

    private int GetSelectedGameIndexPref()
    {
        int selectedGameIndex = -1;
        N64GsUserPrefs? prefs = N64Data?.GsUserPrefs;
        if (prefs != null)
        {
            selectedGameIndex = prefs.SelectedGameIndex;
        }

        return selectedGameIndex;
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
        if (N64Data?.KeyCodes.Count > 0)
        {
            Code? matchingKeyCodeInList = N64Data.KeyCodes.ToList().Find(kc =>
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
        s32[] keyCodeByteLengths = new s32[]
        {
            listBytes.Find("Mario"),
            listBytes.Find("Diddy"),
            listBytes.Find("Yoshi"),
            listBytes.Find("Zelda"),
        }.Where(idx => idx > -1).ToArray();

        s32 keyCodeByteLength = keyCodeByteLengths.Length > 0 ? keyCodeByteLengths.Min() : -1;

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

    #endregion

    #region Writing

    public void UpdateUserPrefs(N64GsConfigureCmdParams cmdParams)
    {
        if (!Support.SupportsUserPrefs)
            return;

        Parsed.N64Data ??= new N64Data();

        if (cmdParams.ResetUserPrefs.HasValue && cmdParams.ResetUserPrefs.Value)
        {
            Parsed.N64Data.GsUserPrefs = MakePristinePrefs();
            SetSelectedGameIndex(-1);
            return;
        }

        string selectedGame = cmdParams.SelectedGame ?? "";
        if (Regex.IsMatch(selectedGame, "^-?[0-9]+$"))
        {
            int gameIndex = Convert.ToInt32(selectedGame, 10) - 1;
            SetSelectedGameIndex(gameIndex);
        }
        else if (Regex.IsMatch(selectedGame, "^(?:0x)?[0-9a-f]+$", RegexOptions.IgnoreCase))
        {
            int gameIndex = Convert.ToInt32(selectedGame, 16);
            SetSelectedGameIndex(gameIndex);
        }
        else if (selectedGame.Length > 0)
        {
            SetSelectedGameName(selectedGame);
        }

        N64GsUserPrefs prefs = Parsed.N64Data.GsUserPrefs ?? MakePristinePrefs();

        prefs.SelectedGameIndex =
            Games
                .Where(game => game.IsGameActive)
                .Select(game => (int)game.GameIndex)
                .FirstOrDefault(-1)
            ;

        if (cmdParams.IsSoundEnabled.HasValue)
            prefs.IsSoundEnabled = cmdParams.IsSoundEnabled.Value;
        if (cmdParams.IsBgScrollEnabled.HasValue)
            prefs.IsBgScrollEnabled = cmdParams.IsBgScrollEnabled.Value;
        if (cmdParams.IsMenuScrollEnabled.HasValue)
            prefs.IsMenuScrollEnabled = cmdParams.IsMenuScrollEnabled.Value;
        if (cmdParams.BgPattern.HasValue)
            prefs.BgPatternId = cmdParams.BgPattern.Value;
        if (cmdParams.BgColor.HasValue)
            prefs.BgColorId = cmdParams.BgColor.Value;

        if (!string.IsNullOrWhiteSpace(cmdParams.MainMenuTitle) && _mainMenuTitle != null)
        {
            _mainMenuTitle.Value =
                cmdParams.MainMenuTitle.Length > 28
                    ? cmdParams.MainMenuTitle[..28]
                    : cmdParams.MainMenuTitle;

            u8[] ascii = _mainMenuTitle.Value.ToAsciiBytes();
            u8[] cstring = new u8[28];
            System.Buffer.BlockCopy(ascii, 0, cstring, 0, ascii.Length);

            AbstractBinaryScribe scribe = Scribe;
            if (ShellFile != null)
            {
                scribe = new BigEndianScribe(ShellFile.UncompressedBytes);
            }
            scribe.Seek(_mainMenuTitle.Addr.StartIndex);
            scribe.WriteBytes(cstring);

            ReadVersionMetadata();
        }

        if (cmdParams.UpdateTimestamp.HasValue && cmdParams.UpdateTimestamp.Value)
        {
            DateTimeOffset now = DateTimeOffset.Now;

            // Raw value must be 16 bytes or less to fit inside the ROM header.
            Metadata.BuildDateRaw.Value = now.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");

            // Full ISO format
            Metadata.BuildDateIso = now.ToIsoString();
        }

        if (cmdParams.RenameKeyCodes.HasValue && cmdParams.RenameKeyCodes.Value)
        {
            foreach (Code kc in Parsed.N64Data.KeyCodes)
            {
                string codeName = kc.CodeName.Value;
                if (codeName.Contains("Mario"))
                    kc.CodeName = "Mario 64, GoldenEye, & Others".ToRomString();
                if (codeName.Contains("Diddy"))
                    kc.CodeName = "Diddy, Banjo-Kazooie,SmashBros".ToRomString();
                if (codeName.Contains("Yoshi"))
                    kc.CodeName = "Yoshi's Story, F-Zero, Cruis'n".ToRomString();
                if (codeName.Contains("Zelda"))
                    kc.CodeName = "Zelda, Perfect Dark, Tooie, DK".ToRomString();
            }
        }

        if (cmdParams.StartupLogo != null)
        {
            // TODO(CheatoBaggins): Figure out how to read/write logos from v2.4 and earlier
            if (Support.IsFirmwareCompressed)
            {
                Image<Rgba32> img = Image.Load<Rgba32>(cmdParams.StartupLogo.FullName);
                SetStartupLogo(img);
            }
            else
            {
                Printer.PrintHint("This firmware version does not support custom startup logos (yet!).");
            }
        }

        if (cmdParams.StartupTile != null)
        {
            // TODO(CheatoBaggins): Figure out how to read/write logos from v2.4 and earlier
            if (Support.IsFirmwareCompressed)
            {
                Image<Rgba32> img = Image.Load<Rgba32>(cmdParams.StartupTile.FullName);
                SetStartupTile(img);
            }
            else
            {
                Printer.PrintHint("This firmware version does not support custom startup logos (yet!).");
            }
        }
    }

    private void SetSelectedGameName(string selectedGame)
    {
        foreach (Game game in Games)
        {
            game.IsGameActive = game.GameName.Value.Equals(selectedGame, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private void SetSelectedGameIndex(s32 gameIndex)
    {
        foreach (Game game in Games)
        {
            game.IsGameActive = game.GameIndex == gameIndex;
        }
    }

    public override void RecalculateKeyCodes(N64KeyCodeId[]? newCics = null)
    {
        if (N64Data == null)
        {
            return;
        }

        List<Code> oldKeyCodes = N64Data.KeyCodes.ToList();
        N64KeyCodeId[] oldCics = oldKeyCodes.Select(GetCicId).ToArray();

        if (newCics == null || newCics.Length == 0 ||
            newCics.SequenceEqual(new [] { N64KeyCodeId.UnspecifiedKeyCodeId }))
        {
            newCics = oldCics;
        }

        var oldCicSet = new HashSet<N64KeyCodeId>(oldCics);
        var newCicSet = new HashSet<N64KeyCodeId>(newCics);

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
        var newKeyCodes = newCics.Select(cic =>
        {
            var oldIdx = oldCics.ToList().IndexOf(cic);
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

        N64Data.KeyCodes.Clear();
        N64Data.KeyCodes.AddRange(newKeyCodes);
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
        WriteHeader();
        WriteEmbeddedFiles();
        WriteUserPrefs();
        WriteGames();
        // This must happen last because it calculates checksums from the ROM
        // file bytes.
        WriteKeyCodes();
        return this;
    }

    private void WriteHeader()
    {
        Scribe.Seek(BuildTimestampAddr).WriteCString(Metadata.BuildDateRaw?.Value ?? Metadata.BuildDateIso, 16);
        if (Metadata.BrandId == BrandId.Libreshark)
        {
            Scribe.Seek(0x100)
                .WriteFixedLengthCString("https://n64brew.dev/", 64)
                .WriteFixedLengthCString("https://github.com/LibreShark", 64)
                .WriteFixedLengthCString("https://github.com/Jhynjhiruu", 64)
                .WriteFixedLengthCString("https://github.com/RWeick", 64)
                .WriteFixedLengthCString("https://github.com/CheatoBaggins", 64);
        }
    }

    public void SetStartupTile(Image<Rgba32> image)
    {
        EmbeddedFile[] tileFiles = _rootCompressedFiles.Where(IsBackgroundTile).ToArray();
        if (tileFiles.Length != 1)
        {
            return;
        }

        if (image.Width != 64 || image.Height != 48)
        {
            string dims = $"{image.Width}x{image.Height}";
            throw new ArgumentException(
                $"Background tiles must be exactly 64x48, but got {dims}."
            );
        }

        var gsImg = new N64GsImageEncoder();
        u8[] bytes = gsImg.EncodeTileGraphic(image);
        tileFiles[0].SetUncompressedBytes(bytes);
    }

    public void SetStartupLogo(Image<Rgba32> inputImage)
    {
        EmbeddedFile[] logoFiles = _rootCompressedFiles.Where(IsStartupLogo).ToArray();
        if (logoFiles.Length != 2)
        {
            return;
        }

        EmbeddedFile? paletteFile = logoFiles.FirstOrDefault(file => file.FileName.EndsWith(".pal"));
        EmbeddedFile? dataFile = logoFiles.FirstOrDefault(file => file.FileName.EndsWith(".bin"));
        if (paletteFile == null || dataFile == null)
        {
            return;
        }

        Image<Rgba32> outputImage = inputImage.Clone();

        // TODO(CheatoBaggins): Move this to an Images class
        string version = Metadata.DisplayVersion;
        string date = DateTime.Parse(Metadata.BuildDateIso).ToString("yyyy-MM-dd");
        string text = $"{version} {date}";
        DrawText(outputImage, text, textOptions =>
        {
            textOptions.Origin = new PointF(296, 138);
            textOptions.VerticalAlignment = VerticalAlignment.Bottom;
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.TextAlignment = TextAlignment.End;
        });

        var n64ImageEncoder = new N64GsImageEncoder();
        (u8[] paletteBytes, u8[] dataBytes) = n64ImageEncoder.EncodeStartupLogo(outputImage);
        paletteFile.SetUncompressedBytes(paletteBytes);
        dataFile.SetUncompressedBytes(dataBytes);
    }

    // TODO(CheatoBaggins): Move this to an Images class
    private static void DrawText(Image<Rgba32> image, string text, Action<TextOptions> configureFont)
    {
        // TODO(CheatoBaggins): Move this to an Images class
        var fonts = new FontCollection();
        FontFamily family = fonts.Add(new MemoryStream(Resources.FONT_PIXELMIX_STANDARD));
        Font font = family.CreateFont(8, FontStyle.Regular);
        TextOptions textOptions = new(font);
        configureFont(textOptions);
        IBrush brush = Brushes.Solid(Color.DarkGray);
        image.Mutate(x => x.DrawText(textOptions, text, brush));
    }

    private static bool IsBackgroundTile(EmbeddedFile file)
    {
        string name = file.FileName;
        return name is "tile1.tg~";
    }

    private static bool IsStartupLogo(EmbeddedFile file)
    {
        string name = file.FileName;
        // Equalizer is the only brand whose logo filename doesn't contain
        // the world "logo". All others are "gslogo2", "gslogo3", "arlogo3",
        // "gblogo3", "lslogo4", etc.
        return name.Contains("logo") || name.Contains("equal2") || name.Contains("equal3");
    }

    private void WriteEmbeddedFiles()
    {
        if (!Support.IsFirmwareCompressed || _rootCompressedFiles.Count == 0)
        {
            // TODO(CheatoBaggins): Figure out how to write images to
            // uncompressed ROMs (v2.4 and earlier).
            return;
        }

        u32 startIndex = _rootCompressedFiles.First().PositionInParentFile.StartIndex;
        u32 endIndex = _rootCompressedFiles.Last().PositionInParentFile.EndIndex;
        Scribe.Seek(startIndex);
        foreach (EmbeddedFile file in _rootCompressedFiles)
        {
            file.Compress();
            u32 structLen = (u32)(4 + 12 + file.CompressedBytes.Length);
            u8[] ascii = file.FileName.ToAsciiBytes();
            u8[] cstring = new u8[12];
            System.Buffer.BlockCopy(ascii, 0, cstring, 0, ascii.Length);

            Scribe.WriteU32(structLen);
            Scribe.WriteBytes(cstring);
            Scribe.WriteBytes(file.CompressedBytes);
        }

        // Padding
        while (Scribe.Position < endIndex)
        {
            Scribe.WriteU8(0xFF);
        }
    }

    private void WriteKeyCodes()
    {
        if (N64Data == null)
        {
            return;
        }

        // Recalculate CRCs and write key codes to list
        // TODO(CheatoBaggins): What about v2.x ROMs with 9-byte key codes?
        Scribe.Seek(_keyCodeListAddr);
        foreach (Code kc in N64Data.KeyCodes)
        {
            u8[] bytes = N64GsChecksum.ComputeKeyCode(Buffer, GetCicId(kc));

            kc.Bytes = ByteString.CopyFrom(bytes);
            kc.Formatted = kc.Bytes.ToCodeString(ThisConsoleId);

            u32 startAddr = Scribe.Position;
            Scribe.WriteBytes(bytes);
            Scribe.WriteCString(kc.CodeName, KeyCodeNameMaxLen, true);

            // Each key code entry is padded to 0x2C bytes long.
            while (Scribe.Position < startAddr + 0x2C)
            {
                Scribe.WriteU8(0);
            }
        }

        // TODO(CheatoBaggins): What about v2.x ROMs with 9-byte key codes?
        Code activeKC = N64Data.KeyCodes.First(kc => kc.IsActiveKeyCode);
        u8[] kcBytes = activeKC.Bytes.ToByteArray();
        u8[] checksum = kcBytes[..8];
        u8[] entrypoint = kcBytes[8..12];
        Scribe.Seek(ActiveKeyCodeAddr).WriteBytes(checksum);
        Scribe.Seek(ProgramCounterAddr).WriteBytes(entrypoint);
    }

    private void WriteUserPrefs()
    {
        N64GsUserPrefs? prefs = N64Data?.GsUserPrefs;
        if (!Support.SupportsUserPrefs)
        {
            return;
        }

        // Perform a factory reset
        if (prefs == null || prefs.Equals(MakePristinePrefs()))
        {
            var padding = new u8[109];
            for (int i = 0; i < padding.Length; i++)
            {
                padding[i] = 0xFF;
            }
            Scribe.Seek(_userPrefsAddr);
            Scribe.WriteBytes(padding);
            return;
        }

        if (Support.HasPristineUserPrefs)
        {
            // A pristine user prefs section contains all 0xFF bytes.
            // Write null bytes to signal that the user has configured settings.
            Scribe.Seek(_userPrefsAddr);
            Scribe.WriteBytes(new u8[109]);
            Support.HasPristineUserPrefs = false;
        }

        Scribe.Seek(_userPrefsAddr)
            .WriteCString("GT", 2, false)
            .WriteBool(prefs.IsSoundEnabled)
            .WriteU8((u8)prefs.BgPatternId)
            .WriteU8((u8)prefs.BgColorId)
            .WriteU8((u8)(prefs.SelectedGameIndex + 1)) // value is 1-indexed
            .Skip(1)
            .WriteBool(prefs.IsBgScrollEnabled)
            .Skip(3)
            .WriteBytes(new u8[] { 0x01, 0x01 }) // TODO(CheatoBaggins): What is this?
            .Skip(9)
            .WriteBytes(new u8[] { 0x01, 0x01 }) // TODO(CheatoBaggins): What is this?
            .Skip(8)
            .WriteBytes(new u8[] { 0x01, 0x01 }) // TODO(CheatoBaggins): What is this?
            .Skip(74)
            .WriteBool(prefs.IsMenuScrollEnabled)
            ;
    }

    private void WriteGames()
    {
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
    }

    private static N64GsUserPrefs MakePristinePrefs()
    {
        return new N64GsUserPrefs()
        {
            // -1 indicates that no game is selected.
            SelectedGameIndex = -1,
            BgPatternId = Nn64GsBgPatternId.Rough,
            BgColorId = Nn64GsBgColorId.Grey,
            IsSoundEnabled = true,
            IsBgScrollEnabled = true,
            IsMenuScrollEnabled = true,
        };
    }

    private static N64KeyCodeId GetCicId(Code kc)
    {
        string name = kc.CodeName.Value.ToUpperInvariant();

        if (name.Contains("DIDDY"))
            return N64KeyCodeId.Diddy;
        if (name.Contains("YOSHI"))
            return N64KeyCodeId.Yoshi;
        if (name.Contains("ZELDA"))
            return N64KeyCodeId.Zelda;
        else
            return N64KeyCodeId.Mario;
    }

    #endregion

    #region Detection

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

    #endregion

    #region Encryption/decryption

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

    #endregion

    #region Printing

    public override void PrintCustomHeader(ICliPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading("Addresses");
        PrintAddressTable(printer);

        printer.PrintHeading("Key codes");
        printer.PrintN64ActiveKeyCode(_activeKeyCode);
        if (Support.HasKeyCodes)
        {
            Console.WriteLine();
            PrintKeyCodesTable(printer);
        }
        else
        {
            printer.PrintHint("This firmware version does not support additional key codes.");
        }

        printer.PrintHeading("User preferences");
        if (Support.SupportsUserPrefs)
        {
            PrintUserPrefsTable(printer);
        }
        else
        {
            printer.PrintHint("This firmware version does not support user preferences.");
        }
    }

    private void PrintUserPrefsTable(ICliPrinter printer)
    {
        var table = printer.BuildTable();
        table.AddColumns("Name", "Value");
        N64GsUserPrefs? prefs = N64Data?.GsUserPrefs;
        if (prefs == null)
        {
            return;
        }

        int selectedGameIndex = prefs.SelectedGameIndex;
        string selectedGameName =
            selectedGameIndex == -1
                ? ""
                : Games.FirstOrDefault(game => game.GameIndex == selectedGameIndex)?.GameName.Value ?? "";
        if (string.IsNullOrEmpty(selectedGameName))
        {
            selectedGameName = printer.Dim("No game selected");
        }
        else
        {
            selectedGameName = printer.BoldUnderline(printer.Green(selectedGameName));
        }

        string enabled = "Enabled";
        string disabled = printer.Dim("Disabled");
        table.AddRow("Selected game", selectedGameName);
        table.AddRow("Background pattern", BgPattern(prefs.BgPatternId));
        table.AddRow("Background color", BgColor(prefs.BgColorId));
        table.AddRow("Sound", prefs.IsSoundEnabled ? enabled : disabled);
        table.AddRow("Background scrolling", prefs.IsBgScrollEnabled ? enabled : disabled);
        table.AddRow("Menu scrolling", prefs.IsMenuScrollEnabled ? enabled : disabled);
        printer.PrintTable(table);
    }

    private string BgPattern(Nn64GsBgPatternId patternId)
    {
        return patternId.ToString();
    }

    private string BgColor(Nn64GsBgColorId colorId)
    {
        var str = colorId.ToString();
        if (colorId == Nn64GsBgColorId.Grey)
        {
            return $"[gray]{str}[/]";
        }
        if (colorId == Nn64GsBgColorId.Blue)
        {
            return $"[blue]{str}[/]";
        }
        if (colorId == Nn64GsBgColorId.Green)
        {
            return $"[green]{str}[/]";
        }
        if (colorId == Nn64GsBgColorId.Red)
        {
            return $"[red]{str}[/]";
        }
        if (colorId == Nn64GsBgColorId.Yellow)
        {
            return $"[yellow]{str}[/]";
        }
        if (colorId == Nn64GsBgColorId.Pink)
        {
            return $"[bold red]{str}[/]";
        }
        if (colorId == Nn64GsBgColorId.Tan)
        {
            return $"[tan]{str}[/]";
        }
        return str;
    }

    private void PrintAddressTable(ICliPrinter printer)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Section"))
                .AddColumn(printer.HeaderCell("Address"))
            ;

        string firmwareAddr = $"0x{_firmwareAddr:X8}";
        string gameListAddr = $"0x{_gameListAddr:X8}";
        string keyCodeListAddr = Support.HasKeyCodes ? $"0x{_keyCodeListAddr:X8}" : "Not supported";
        string userPrefsAddr = Support.SupportsUserPrefs ? $"0x{_userPrefsAddr:X8}" : "Not supported";

        table.AddRow("Firmware", firmwareAddr);
        table.AddRow("User prefs", userPrefsAddr);
        table.AddRow("Key code list", keyCodeListAddr);
        table.AddRow("Game list", gameListAddr);

        printer.PrintTable(table);
    }

    private void PrintKeyCodesTable(ICliPrinter printer)
    {
        if (N64Data == null)
        {
            return;
        }

        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Games"))
                .AddColumn(printer.HeaderCell("Key code"))
                .AddColumn(printer.HeaderCell("CIC chips"))
            ;

        foreach (Code keyCode in N64Data.KeyCodes)
        {
            string keyCodeName = printer.FormatN64KeyCodeName(keyCode);
            string hexString = printer.FormatN64KeyCodeBytes(keyCode, _activeKeyCode);
            string codeName = keyCode.CodeName.Value;
            string cicChips = "";
            if (codeName.Contains("Diddy"))
                cicChips = "6103, 7103";
            else if (codeName.Contains("Yoshi"))
                cicChips = "6106, 7106";
            else if (codeName.Contains("Zelda"))
                cicChips = "6105, 7105";
            else
                cicChips = "6101, 6102, 7101, 7102";
            table.AddRow(
                keyCode.IsActiveKeyCode
                    ? $"> {keyCodeName} " + printer.Italic("(active)")
                    : $"  {keyCodeName}",
                hexString,
                cicChips
            );
        }

        printer.PrintTable(table);
    }

    #endregion
}
