using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;

namespace LibreShark.Hammerhead;

public abstract class CmdParams : EventArgs
{
    public bool HideBanner { get; set; }
    public bool Clean { get; set; }
    public PrintFormat PrintFormat { get; set; }
}

public class InfoCmdParams : CmdParams
{
    public FileFormat InputFormat { get; set; }
    public bool HideGames { get; set; }
    public bool HideCheats { get; set; }
    public bool HideCodes { get; set; }
    public FileInfo[] InputFiles { get; set; }

    public InfoCmdParams()
    {
        InputFiles = new FileInfo[] { };
    }
}

public class RomCmdParams : CmdParams
{
    public FileInfo? InputFile { get; set; }
    public FileInfo? OutputFile { get; set; }
}

public class CheatsCmdParams : CmdParams
{
    public FileInfo? InputFile { get; set; }
    public FileInfo[] InputFiles { get; set; }
    public FileInfo? OutputFile { get; set; }

    public CheatsCmdParams()
    {
        InputFiles = new FileInfo[] { };
    }
}

public class Cli
{
    #region Options and arguments

    private static readonly Option<bool> HideBannerOption = new Option<bool>(
        aliases: new string[] { "--hide-banner" },
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

    private static readonly Option<FileFormat> InputFormatOption = new Option<FileFormat>(
        aliases: new string[] { "--input-format" },
        description: "Force Hammerhead to use a specific file format when reading input files.",
        getDefaultValue: () => FileFormat.Auto
    )
    {
        ArgumentHelpName = "auto|rom|json|proto|n64_datel_text|n64_edx7|n64_pj64_v3",
    };

    private static readonly Option<FileFormat> DumpCheatsOutputFormatOption = new Option<FileFormat>(
        aliases: new string[] { "--output-format" },
        description: "Force Hammerhead to use a specific file format when writing output files.",
        getDefaultValue: () => FileFormat.Auto
    )
    {
        ArgumentHelpName = "auto|json|proto|n64_datel_text|n64_edx7|n64_pj64_v3",
    };

    private static readonly Option<FileFormat> CopyCheatsOutputFormatOption = new Option<FileFormat>(
        aliases: new string[] { "--output-format" },
        description: "Force Hammerhead to use a specific file format when writing output files.",
        getDefaultValue: () => FileFormat.Auto
    )
    {
        ArgumentHelpName = "auto|rom|json|proto|n64_datel_text|n64_edx7|n64_pj64_v3",
    };

    private static readonly Option<bool?> ColorOption = new Option<bool?>(
        aliases: new string[] { "--color" },
        description: "Force Hammerhead to output ANSI color code escape sequences when printing to stdout.",
        getDefaultValue: () => null
    );

    private static readonly Option<bool?> NoColorOption = new Option<bool?>(
        aliases: new string[] { "--no-color" },
        description: "Force Hammerhead to disable ANSI color code escape sequences when printing to stdout.",
        getDefaultValue: () => null
    );

    private static readonly Option<bool> HideGamesOption = new Option<bool>(
        aliases: new string[] { "--hide-games" },
        description: "Do not print games to the console."
    );

    private static readonly Option<bool> HideCheatsOption = new Option<bool>(
        aliases: new string[] { "--hide-cheats" },
        description: "Do not print cheats to the console."
    );

    private static readonly Option<bool> HideCodesOption = new Option<bool>(
        aliases: new string[] { "--hide-codes" },
        description: "Do not print codes to the console."
    );

    private static readonly Argument<FileInfo[]> InputFilesArgument = new Argument<FileInfo[]>(
        "input_files",
        "One or more firmware dumps (ROM files) or cheat lists to read.")
    {
        Arity = ArgumentArity.OneOrMore,
    };

    private static readonly Argument<FileInfo> InputFileArgument = new Argument<FileInfo>(
        "input_file",
        "Path to a ROM file to read.")
    {
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<FileInfo> OutputFileOption = new Option<FileInfo>(
        aliases: new string[] { "-o", "--output-file" },
        "Optional path to the output file to write.\n" +
        "If not specified, a reasonable default filename will be auto-generated.")
    {
        Arity = ArgumentArity.ZeroOrOne,
        ArgumentHelpName = "output_file",
    };

    private static readonly Option<DirectoryInfo> OutputDirOption = new Option<DirectoryInfo>(
        aliases: new string[] { "-o", "--output-dir" },
        "Optional path to a directory to write output files in.\n" +
        "If not specified, output files will be written to the same directory as the corresponding input file.")
    {
        Arity = ArgumentArity.ZeroOrOne,
        ArgumentHelpName = "output_dir",
    };

    #endregion

    #region Root command

    private readonly RootCommand _rootCmd = new(
        description: string.Join(" ", new string[]
        {
            "Swiss Army Knife for reading, writing, encrypting, and decrypting firmware dumps (ROM files)",
            "and cheat code lists from 1990s-2000s video game enhancers (GameShark, Action Replay, Code Breaker,",
            "Xplorer/Xploder, etc.).",
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

    private readonly Command _romCmd = new Command(
        "rom",
        "Read, write, encrypt, decrypt, and edit ROM files (firmware dumps).");

    private readonly Command _encryptRomCmd = new Command(
        "encrypt",
        "Encrypt a ROM file " +
        "for compatibility with chip flashers and the manufacturer's official PC update utilities.\n" +
        "If the ROM format does not support encryption, " +
        "the output file will be a 1:1 copy of the input.")
    {
        OutputFileOption,
        OverwriteOption,
        InputFileArgument,
    };

    private readonly Command _decryptRomCmd = new Command(
        "decrypt",
        "Decrypt a ROM file so that it may be edited directly.\n" +
        "If the ROM format does not support encryption, " +
        "the output file will be a 1:1 copy of the input.")
    {
        OutputFileOption,
        OverwriteOption,
        InputFileArgument,
    };

    private readonly Command _scrambleRomCmd = new Command(
        "scramble",
        "Scramble (reorder) the bytes in a ROM file " +
        "for compatibility with official PC update utilities and chip writers.\n" +
        "If the ROM format does not support scrambling, " +
        "the output file will be a 1:1 copy of the input.")
    {
        OutputFileOption,
        OverwriteOption,
        InputFileArgument,
    };

    private readonly Command _unscrambleRomCmd = new Command(
        "unscramble",
        "Unscramble (reorder) the bytes in a ROM file.\n" +
        "If the ROM format does not support scrambling, " +
        "the output file will be a 1:1 copy of the input.")
    {
        OutputFileOption,
        OverwriteOption,
        InputFileArgument,
    };

    private readonly Command _splitRomCmd = new Command(
        "split",
        "Split a ROM file into sections (e.g., header, firmware, key codes, user prefs, and cheat list) " +
        "and write each section to a separate output file.")
    {
        OutputFileOption,
        OverwriteOption,
        InputFileArgument,
    };

    private readonly Command _combineRomCmd = new Command(
        "combine",
        "Combine ROM sections into a single ROM file.")
    {
        OutputFileOption,
        OverwriteOption,
        InputFilesArgument,
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
        OutputFileOption,
        InputFormatOption,
        CopyCheatsOutputFormatOption,
        OverwriteOption,
        InputFileArgument,
    };

    #endregion

    public event EventHandler<CmdParams>? Always;
    public event EventHandler<InfoCmdParams>? OnInfo;
    public event EventHandler<RomCmdParams>? OnEncryptRom;
    public event EventHandler<RomCmdParams>? OnDecryptRom;
    public event EventHandler<RomCmdParams>? OnScrambleRom;
    public event EventHandler<RomCmdParams>? OnUnscrambleRom;
    public event EventHandler<RomCmdParams>? OnSplitRom;
    public event EventHandler<RomCmdParams>? OnCombineRom;
    public event EventHandler<CheatsCmdParams>? CheatsCmdHandlers;

    public RootCommand RootCommand => _rootCmd;

    public Cli()
    {
        _rootCmd.AddGlobalOption(HideBannerOption);
        _rootCmd.AddGlobalOption(NoColorOption);
        _rootCmd.AddGlobalOption(ColorOption);
        _rootCmd.AddGlobalOption(CleanOption);

        _rootCmd.AddCommand(_infoCmd);
        _rootCmd.AddCommand(_romCmd);
        _rootCmd.AddCommand(_cheatsCmd);

        _romCmd.AddCommand(_encryptRomCmd);
        _romCmd.AddCommand(_decryptRomCmd);
        _romCmd.AddCommand(_scrambleRomCmd);
        _romCmd.AddCommand(_unscrambleRomCmd);
        // TODO(CheatoBaggins): Implement
        // _romCmd.AddCommand(_splitRomCmd);
        // _romCmd.AddCommand(_combineRomCmd);

        _cheatsCmd.AddCommand(_dumpCheatsCmd);
        _cheatsCmd.AddCommand(_copyCheatsCmd);

        _infoCmd.Handler = new AnonymousCliCommandHandler((ctx) =>
        {
            var cmdParams = new InfoCmdParams()
            {
                // Global options
                PrintFormat = GetPrintFormat(ctx),
                HideBanner = HideBannerOption.GetValue(ctx),
                Clean = CleanOption.GetValue(ctx),

                // Command-specific options
                InputFormat = InputFormatOption.GetValue(ctx),
                HideGames = HideGamesOption.GetValue(ctx),
                HideCheats = HideCheatsOption.GetValue(ctx),
                HideCodes = HideCodesOption.GetValue(ctx),

                // Command-specific arguments
                InputFiles = InputFilesArgument.GetValue(ctx)!,
            };
            Always?.Invoke(this, cmdParams);
            OnInfo?.Invoke(this, cmdParams);
        });

        _encryptRomCmd.Handler = new AnonymousCliCommandHandler((ctx) =>
        {
            var cmdParams = new RomCmdParams()
            {
                // Global options
                PrintFormat = GetPrintFormat(ctx),
                HideBanner = HideBannerOption.GetValue(ctx),
                Clean = CleanOption.GetValue(ctx),

                // Command-specific arguments
                InputFile = InputFileArgument.GetValue(ctx)!,
                OutputFile = OutputFileOption.GetValue(ctx),
            };
            Always?.Invoke(this, cmdParams);
            OnEncryptRom?.Invoke(this, cmdParams);
        });

        _decryptRomCmd.Handler = new AnonymousCliCommandHandler((ctx) =>
        {
            var cmdParams = new RomCmdParams()
            {
                // Global options
                PrintFormat = GetPrintFormat(ctx),
                HideBanner = HideBannerOption.GetValue(ctx),
                Clean = CleanOption.GetValue(ctx),

                // Command-specific arguments
                InputFile = InputFileArgument.GetValue(ctx)!,
                OutputFile = OutputFileOption.GetValue(ctx),
            };
            Always?.Invoke(this, cmdParams);
            OnDecryptRom?.Invoke(this, cmdParams);
        });

        _scrambleRomCmd.Handler = new AnonymousCliCommandHandler((ctx) =>
        {
            var cmdParams = new RomCmdParams()
            {
                // Global options
                PrintFormat = GetPrintFormat(ctx),
                HideBanner = HideBannerOption.GetValue(ctx),
                Clean = CleanOption.GetValue(ctx),

                // Command-specific arguments
                InputFile = InputFileArgument.GetValue(ctx)!,
                OutputFile = OutputFileOption.GetValue(ctx),
            };
            Always?.Invoke(this, cmdParams);
            OnScrambleRom?.Invoke(this, cmdParams);
        });

        _unscrambleRomCmd.Handler = new AnonymousCliCommandHandler((ctx) =>
        {
            var cmdParams = new RomCmdParams()
            {
                // Global options
                PrintFormat = GetPrintFormat(ctx),
                HideBanner = HideBannerOption.GetValue(ctx),
                Clean = CleanOption.GetValue(ctx),

                // Command-specific arguments
                InputFile = InputFileArgument.GetValue(ctx)!,
                OutputFile = OutputFileOption.GetValue(ctx),
            };
            Always?.Invoke(this, cmdParams);
            OnUnscrambleRom?.Invoke(this, cmdParams);
        });

        _splitRomCmd.Handler = new AnonymousCliCommandHandler((ctx) =>
        {
            var cmdParams = new RomCmdParams()
            {
                // Global options
                PrintFormat = GetPrintFormat(ctx),
                HideBanner = HideBannerOption.GetValue(ctx),
                Clean = CleanOption.GetValue(ctx),

                // Command-specific arguments
                InputFile = InputFileArgument.GetValue(ctx)!,
                OutputFile = OutputFileOption.GetValue(ctx),
            };
            Always?.Invoke(this, cmdParams);
            OnSplitRom?.Invoke(this, cmdParams);
        });

        _combineRomCmd.Handler = new AnonymousCliCommandHandler((ctx) =>
        {
            var cmdParams = new RomCmdParams()
            {
                // Global options
                PrintFormat = GetPrintFormat(ctx),
                HideBanner = HideBannerOption.GetValue(ctx),
                Clean = CleanOption.GetValue(ctx),

                // Command-specific arguments
                InputFile = InputFileArgument.GetValue(ctx)!,
                OutputFile = OutputFileOption.GetValue(ctx),
            };
            Always?.Invoke(this, cmdParams);
            OnCombineRom?.Invoke(this, cmdParams);
        });
    }

    private static PrintFormat GetPrintFormat(InvocationContext ctx)
    {
        var printFormat = PrintFormat.Detect;
        if (NoColorOption.GetValue(ctx) == true)
        {
            printFormat = PrintFormat.Plain;
        }
        else if (ColorOption.GetValue(ctx) == true)
        {
            printFormat = PrintFormat.Color;
        }

        return printFormat;
    }
}
