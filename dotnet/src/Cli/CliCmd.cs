using System.CommandLine;
using System.CommandLine.Invocation;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Nintendo64;

namespace LibreShark.Hammerhead.Cli;

public class CliCmd
{
    #region Options and arguments

    private static readonly Option<bool> HideBannerOption = new Option<bool>(
        aliases: new string[] { "--no-banner" },
        description: "Disable the decorative ASCII art banner."
    );

    private static readonly Option<bool> CleanOption = new Option<bool>(
        aliases: new string[] { "--clean" },
        description: "Attempt to remove invalid cheats, sort the game list, reset user preferences, etc."
    ) { IsHidden = true };

    private static readonly Option<bool> OverwriteOption = new Option<bool>(
        aliases: new string[] { "-y", "--overwrite" },
        description: "Overwrite existing output files without prompting."
    );

    private static readonly Option<CodecId> InputFormatOption = new Option<CodecId>(
        aliases: new string[] { "--input-format" },
        description: "Force Hammerhead to use a specific file format when reading input files.",
        getDefaultValue: () => CodecId.Auto
        // parseArgument: (x) => x
    );

    private static readonly Option<CodecId> DumpCheatsOutputFormatOption = new Option<CodecId>(
        aliases: new string[] { "--output-format" },
        description: "Force Hammerhead to use a specific file format when writing output files.",
        // parseArgument: (x) => x,
        getDefaultValue: () => CodecId.Auto
    );

    private static readonly Option<CodecId> CopyCheatsOutputFormatOption = new Option<CodecId>(
        aliases: new string[] { "--output-format" },
        description: "Force Hammerhead to use a specific file format when writing output files.",
        // parseArgument: (x) => x,
        getDefaultValue: () => CodecId.Auto
    );

    private static readonly Option<PrintFormatId> PrintFormatIdOption = new Option<PrintFormatId>(
        aliases: new string[] { "--print-format" },
        description: "Force Hammerhead to print to stdout using the specified format.",
        getDefaultValue: () => PrintFormatId.Detect
    );

    private static readonly Option<bool?> ColorOption = new Option<bool?>(
        aliases: new string[] { "--color" },
        description: "Force Hammerhead to output ANSI color code escape sequences when printing to stdout."
    );

    private static readonly Option<bool?> NoColorOption = new Option<bool?>(
        aliases: new string[] { "--no-color" },
        description: "Force Hammerhead to disable ANSI color code escape sequences when printing to stdout."
    );

    private static readonly Option<bool> HideGamesOption = new Option<bool>(
        aliases: new string[] { "--no-games" },
        description: "Do not print games to the console."
    );

    private static readonly Option<bool> HideCheatsOption = new Option<bool>(
        aliases: new string[] { "--no-cheats" },
        description: "Do not print cheats to the console."
    );

    private static readonly Option<bool> HideCodesOption = new Option<bool>(
        aliases: new string[] { "--no-codes" },
        description: "Do not print codes to the console."
    );

    private static readonly Argument<FileInfo> InputFileArgument = new Argument<FileInfo>(
        "input_file",
        "Path to an input file to read.");

    private static readonly Argument<FileInfo[]> InputFilesArgument = new Argument<FileInfo[]>(
        "input_files",
        "One or more input files to read.")
    {
        Arity = ArgumentArity.OneOrMore,
    };

    private static readonly Argument<FileInfo> OutputFileArgument = new Argument<FileInfo>(
        name: "output_file",
        "Optional path to the output file to write.\n" +
        "If not specified, a reasonable default filename will be auto-generated.")
    {
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<FileInfo> OutputFileOption = new Option<FileInfo>(
        aliases: new string[] { "-o", "--output-file" },
        "Optional path to the output file to write.\n" +
        "If not specified, a reasonable default filename will be auto-generated.")
    {
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<DirectoryInfo> OutputDirOption = new Option<DirectoryInfo>(
        aliases: new string[] { "-d", "--output-dir" },
        "Optional path to a directory to write output files in.\n" +
        "If not specified, output files will be written to the same directory " +
        "as the corresponding input file.")
    {
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<string> N64KeyCodeOrderOption = new Option<string>(
        aliases: new string[] { "--keycode-order" },
        "Comma-separated list of key code IDs. E.g., 'zelda,mario,diddy,yoshi'.");

    private static readonly Option<string> N64SelectedGameOption = new Option<string>(
        aliases: new string[] { "--selected-game" },
        "Specify the name or zero-based index of a game " +
        "to automatically select when the user turns on the GS. " +
        "Pass -1 to reset the selected game to none.");

    private static readonly Option<bool?> N64SoundOption = new Option<bool?>(
        aliases: new string[] { "--sound" },
        "Enable or disable menu sounds in the UI.");

    private static readonly Option<bool?> N64MenuScrollOption = new Option<bool?>(
        aliases: new string[] { "--menu-scroll" },
        "Enable or disable menu scrolling in the UI.");

    private static readonly Option<bool?> N64BgScrollOption = new Option<bool?>(
        aliases: new string[] { "--bg-scroll" },
        "Enable or disable background scrolling in the UI.");

    private static readonly Option<Nn64GsBgPatternId?> N64BgPatternOption = new Option<Nn64GsBgPatternId?>(
        aliases: new string[] { "--bg-pattern" },
        "Set the background tile pattern in the UI.");

    private static readonly Option<Nn64GsBgColorId?> N64BgColorOption = new Option<Nn64GsBgColorId?>(
        aliases: new string[] { "--bg-color" },
        "Set the background tile color in the UI.");

    private static readonly Option<bool?> N64UpdateTimestampOption = new Option<bool?>(
        aliases: new string[] { "--update-timestamp" },
        "Enable or disable updating the build date timestamp in the ROM file header.");

    private static readonly Option<bool?> N64RenameKeyCodesOption = new Option<bool?>(
        aliases: new string[] { "--rename-keycodes" },
        "Enable or disable automatically renaming key codes to be clearer.");

    private static readonly Option<bool?> N64ResetUserPrefsOption = new Option<bool?>(
        aliases: new string[] { "--reset-prefs" },
        "Clear all user-configurable preferences and restore them to factory defaults.");

    #endregion

    #region Root command

    private readonly RootCommand _rootCmd = new(
        description: string.Join(" ", new string[]
        {
            "Hammerhead is a Swiss Army Knife for reading, writing, encrypting, and decrypting firmware dumps",
            "(ROM files) and cheat code lists from 1990s-2000s video game enhancers (GameShark, Action Replay,",
            "Code Breaker, Xplorer/Xploder, etc.).",
        }));

    #endregion

    #region `info` command

    private readonly Command _infoCmd = new Command(
        "info",
        "Display detailed information about ROM and cheat list files.")
    {
        InputFormatOption,
        HideGamesOption,
        HideCheatsOption,
        HideCodesOption,
        InputFilesArgument,
    };

    #endregion

    #region `rom` commands

    private readonly Command _encryptRomCmd = new Command(
        "encrypt",
        "Encrypt a ROM file " +
        "for compatibility with chip flashers and the manufacturer's official PC update utilities.")
    {
        OverwriteOption,
        InputFileArgument,
        OutputFileArgument,
    };

    private readonly Command _decryptRomCmd = new Command(
        "decrypt",
        "Decrypt a ROM file so that it can be viewed in a hex editor " +
        "or manipulated by other tools that don't support encryption.")
    {
        OverwriteOption,
        InputFileArgument,
        OutputFileArgument,
    };

    private readonly Command _scrambleRomCmd = new Command(
        "scramble",
        "Scramble (reorder) the bytes in a ROM file " +
        "for compatibility with official PC update utilities and chip writers.")
    {
        OverwriteOption,
        InputFileArgument,
        OutputFileArgument,
    };

    private readonly Command _unscrambleRomCmd = new Command(
        "unscramble",
        "Unscramble (reorder) the bytes in a ROM file.")
    {
        OverwriteOption,
        InputFileArgument,
        OutputFileArgument,
    };

    private readonly Command _extractRomCmd = new Command(
        "extract",
        "Extract embedded files from the given ROM(s).")
    {
        OverwriteOption,
        InputFilesArgument,
        OutputFileOption,
    };

    #endregion

    #region `cheats` commands

    private readonly Command _cheatsCmd = new Command(
        "cheats",
        "Read, write, copy, clean, and convert cheat code lists.");

    private readonly Command _dumpCheatsCmd = new Command(
        "dump",
        "Read and decrypt all cheats from one or more ROM files, and write them to formatted text files on disk.")
    {
        OutputDirOption,
        DumpCheatsOutputFormatOption,
        OverwriteOption,
        InputFilesArgument,
    };

    private readonly Command _copyCheatsCmd = new Command(
        "copy",
        "Import/export all cheats from one ROM file or cheat list to another.")
    {
        InputFormatOption,
        CopyCheatsOutputFormatOption,
        OverwriteOption,
        InputFileArgument,
        OutputFileArgument,
    };

    #endregion

    #region `n64` commands

    private readonly Command _n64Cmd = new Command(
        "n64",
        "Nintendo 64-specific utilities.");

    private readonly Command _n64GsCmd = new Command(
        "gs",
        "GameShark-specific utilities.");

    private readonly Command _n64XpCmd = new Command(
        "xp",
        "Xplorer 64-specific utilities.");

    private readonly Command _n64GsConfigureCmd = new Command(
        "config",
        "Configure user preferences.")
    {
        OverwriteOption,
        InputFileArgument,
        OutputFileArgument,
        N64KeyCodeOrderOption,
        N64ResetUserPrefsOption,
        N64SelectedGameOption,
        N64SoundOption,
        N64MenuScrollOption,
        N64BgScrollOption,
        N64BgPatternOption,
        N64BgColorOption,
        N64UpdateTimestampOption,
        N64RenameKeyCodesOption,
    };

    #endregion

    #region Event handlers

    public event EventHandler<CmdParams>? Always;
    public event EventHandler<InfoCmdParams>? OnInfo;
    public event EventHandler<RomCmdParams>? OnEncryptRom;
    public event EventHandler<RomCmdParams>? OnDecryptRom;
    public event EventHandler<RomCmdParams>? OnScrambleRom;
    public event EventHandler<RomCmdParams>? OnUnscrambleRom;
    public event EventHandler<ExtractRomCmdParams>? OnExtractRom;
    public event EventHandler<DumpCheatsCmdParams>? OnDumpCheats;
    public event EventHandler<RomCmdParams>? OnCopyCheats;
    public event EventHandler<N64GsConfigureCmdParams>? OnN64GsConfigure;

    #endregion

    public RootCommand RootCommand => _rootCmd;

    public CliCmd()
    {
        _rootCmd.Add(HideBannerOption);
        _rootCmd.Add(NoColorOption);
        _rootCmd.Add(ColorOption);
        _rootCmd.Add(PrintFormatIdOption);
        _rootCmd.Add(CleanOption);

        _rootCmd.Add(_infoCmd);
        _rootCmd.Add(_cheatsCmd);
        _rootCmd.Add(_n64Cmd);

        _cheatsCmd.Add(_dumpCheatsCmd);
        _cheatsCmd.Add(_copyCheatsCmd);

        _n64Cmd.Add(_n64GsCmd);
        _n64GsCmd.Add(_n64GsConfigureCmd);
        _n64GsCmd.Add(_encryptRomCmd);
        _n64GsCmd.Add(_decryptRomCmd);
        _n64GsCmd.Add(_extractRomCmd);

        _n64Cmd.Add(_n64XpCmd);
        _n64XpCmd.Add(_scrambleRomCmd);
        _n64XpCmd.Add(_unscrambleRomCmd);

        _infoCmd.Handler = new CliCmdHandler(Info);
        _encryptRomCmd.Handler = new CliCmdHandler(EncryptRom);
        _decryptRomCmd.Handler = new CliCmdHandler(DecryptRom);
        _scrambleRomCmd.Handler = new CliCmdHandler(ScrambleRom);
        _unscrambleRomCmd.Handler = new CliCmdHandler(UnscrambleRom);
        _extractRomCmd.Handler = new CliCmdHandler(ExtractRom);
        _dumpCheatsCmd.Handler = new CliCmdHandler(DumpCheats);
        _copyCheatsCmd.Handler = new CliCmdHandler(CopyCheats);
        _n64GsConfigureCmd.Handler = new CliCmdHandler(N64GsConfigure);
    }

    private void N64GsConfigure(InvocationContext ctx)
    {
        var cmdParams = new N64GsConfigureCmdParams()
        {
            // Global options
            PrintFormatId = GetPrintFormatId(ctx),
            HideBanner = HideBannerOption.GetValue(ctx),
            Clean = CleanOption.GetValue(ctx),

            // Command-specific options
            InputFile = InputFileArgument.GetValue(ctx)!,
            OutputFile = OutputFileArgument.GetValue(ctx),
            OverwriteExistingFiles = OverwriteOption.GetValue(ctx),
            KeyCodeIds = (N64KeyCodeOrderOption.GetValue(ctx) ?? "").Split(",").Select(str =>
            {
                Enum.TryParse(str, ignoreCase: true, result: out N64KeyCodeId cic);
                return cic;
            }).ToArray(),

            SelectedGame = N64SelectedGameOption.GetValue(ctx),
            IsSoundEnabled = N64SoundOption.GetValue(ctx),
            IsMenuScrollEnabled = N64MenuScrollOption.GetValue(ctx),
            IsBgScrollEnabled = N64BgScrollOption.GetValue(ctx),
            BgPattern = N64BgPatternOption.GetValue(ctx),
            BgColor = N64BgColorOption.GetValue(ctx),
            UpdateTimestamp = N64UpdateTimestampOption.GetValue(ctx),
            RenameKeyCodes = N64RenameKeyCodesOption.GetValue(ctx),
            ResetUserPrefs = N64ResetUserPrefsOption.GetValue(ctx),
        };
        Always?.Invoke(this, cmdParams);
        OnN64GsConfigure?.Invoke(this, cmdParams);
    }

    #region `info` command

    private void Info(InvocationContext ctx)
    {
        var cmdParams = new InfoCmdParams()
        {
            // Global options
            PrintFormatId = GetPrintFormatId(ctx),
            HideBanner = HideBannerOption.GetValue(ctx),
            Clean = CleanOption.GetValue(ctx),

            // Command-specific options
            InputCodecId = InputFormatOption.GetValue(ctx),
            HideGames = HideGamesOption.GetValue(ctx),
            HideCheats = HideCheatsOption.GetValue(ctx),
            HideCodes = HideCodesOption.GetValue(ctx),

            // Command-specific arguments
            InputFiles = InputFilesArgument.GetValue(ctx)!,
        };
        Always?.Invoke(this, cmdParams);
        OnInfo?.Invoke(this, cmdParams);
    }

    #endregion

    #region `rom` commands

    private void EncryptRom(InvocationContext ctx)
    {
        var cmdParams = new RomCmdParams()
        {
            // Global options
            PrintFormatId = GetPrintFormatId(ctx),
            HideBanner = HideBannerOption.GetValue(ctx),
            Clean = CleanOption.GetValue(ctx),

            // Command-specific arguments
            InputFile = InputFileArgument.GetValue(ctx)!,
            OutputFile = OutputFileArgument.GetValue(ctx),
            OverwriteExistingFiles = OverwriteOption.GetValue(ctx),
        };
        Always?.Invoke(this, cmdParams);
        OnEncryptRom?.Invoke(this, cmdParams);
    }

    private void DecryptRom(InvocationContext ctx)
    {
        var cmdParams = new RomCmdParams()
        {
            // Global options
            PrintFormatId = GetPrintFormatId(ctx),
            HideBanner = HideBannerOption.GetValue(ctx),
            Clean = CleanOption.GetValue(ctx),

            // Command-specific arguments
            InputFile = InputFileArgument.GetValue(ctx)!,
            OutputFile = OutputFileArgument.GetValue(ctx),
            OverwriteExistingFiles = OverwriteOption.GetValue(ctx),
        };
        Always?.Invoke(this, cmdParams);
        OnDecryptRom?.Invoke(this, cmdParams);
    }

    private void ScrambleRom(InvocationContext ctx)
    {
        var cmdParams = new RomCmdParams()
        {
            // Global options
            PrintFormatId = GetPrintFormatId(ctx),
            HideBanner = HideBannerOption.GetValue(ctx),
            Clean = CleanOption.GetValue(ctx),

            // Command-specific arguments
            InputFile = InputFileArgument.GetValue(ctx)!,
            OutputFile = OutputFileArgument.GetValue(ctx),
            OverwriteExistingFiles = OverwriteOption.GetValue(ctx),
        };
        Always?.Invoke(this, cmdParams);
        OnScrambleRom?.Invoke(this, cmdParams);
    }

    private void UnscrambleRom(InvocationContext ctx)
    {
        var cmdParams = new RomCmdParams()
        {
            // Global options
            PrintFormatId = GetPrintFormatId(ctx),
            HideBanner = HideBannerOption.GetValue(ctx),
            Clean = CleanOption.GetValue(ctx),

            // Command-specific arguments
            InputFile = InputFileArgument.GetValue(ctx)!,
            OutputFile = OutputFileArgument.GetValue(ctx),
            OverwriteExistingFiles = OverwriteOption.GetValue(ctx),
        };
        Always?.Invoke(this, cmdParams);
        OnUnscrambleRom?.Invoke(this, cmdParams);
    }

    private void ExtractRom(InvocationContext ctx)
    {
        var cmdParams = new ExtractRomCmdParams()
        {
            // Global options
            PrintFormatId = GetPrintFormatId(ctx),
            HideBanner = HideBannerOption.GetValue(ctx),
            Clean = CleanOption.GetValue(ctx),

            // Command-specific arguments
            InputFiles = InputFilesArgument.GetValue(ctx)!,
            OutputDir = OutputDirOption.GetValue(ctx),
            OverwriteExistingFiles = OverwriteOption.GetValue(ctx),
        };
        Always?.Invoke(this, cmdParams);
        OnExtractRom?.Invoke(this, cmdParams);
    }

    #endregion

    #region `cheats` commands

    private void DumpCheats(InvocationContext ctx)
    {
        var cmdParams = new DumpCheatsCmdParams()
        {
            // Global options
            PrintFormatId = GetPrintFormatId(ctx),
            HideBanner = HideBannerOption.GetValue(ctx),
            Clean = CleanOption.GetValue(ctx),

            // Command-specific arguments
            InputFiles = InputFilesArgument.GetValue(ctx)!,
            OutputDir = OutputDirOption.GetValue(ctx),
            OutputFormat = DumpCheatsOutputFormatOption.GetValue(ctx),
            OverwriteExistingFiles = OverwriteOption.GetValue(ctx),
        };
        Always?.Invoke(this, cmdParams);
        OnDumpCheats?.Invoke(this, cmdParams);
    }

    private void CopyCheats(InvocationContext ctx)
    {
        var cmdParams = new RomCmdParams()
        {
            // Global options
            PrintFormatId = GetPrintFormatId(ctx),
            HideBanner = HideBannerOption.GetValue(ctx),
            Clean = CleanOption.GetValue(ctx),

            // Command-specific arguments
            InputFile = InputFileArgument.GetValue(ctx)!,
            OutputFile = OutputFileArgument.GetValue(ctx),
            OutputFormat = CopyCheatsOutputFormatOption.GetValue(ctx),
            OverwriteExistingFiles = OverwriteOption.GetValue(ctx),
        };
        Always?.Invoke(this, cmdParams);
        OnCopyCheats?.Invoke(this, cmdParams);
    }

    #endregion

    private static PrintFormatId GetPrintFormatId(InvocationContext ctx)
    {
        PrintFormatId printFormat = PrintFormatIdOption.GetValue(ctx);
        if (printFormat == PrintFormatId.Detect)
        {
            if (NoColorOption.GetValue(ctx) == true)
            {
                printFormat = PrintFormatId.Plain;
            }
            else if (ColorOption.GetValue(ctx) == true)
            {
                printFormat = PrintFormatId.Color;
            }
        }
        return TerminalPrinter.GetEffectivePrintFormatId(printFormat);
    }
}
