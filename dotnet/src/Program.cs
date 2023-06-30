using System.CommandLine;
using System.Text.RegularExpressions;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.Codecs;

namespace LibreShark.Hammerhead;

/*

# Global options
--clean
--hide-banner
--overwrite

# Display detailed information about ROM files and cheat list files.
hh info n64-*.bin \
    --input-format=auto|n64_cheats_datel_memcard|... \
    --print-format=auto|color|plain|markdown|jsonproto|textproto|none

# ROMs are automatically decrypted and unscrambled every time they're read in.
hh rom encrypt    ar3.dec               [-o ar3.enc]
hh rom decrypt    ar3.enc               [-o ar3.dec]
hh rom scramble   xplorer64.dec         [-o xplorer64.enc]
hh rom unscramble xplorer64.enc         [-o xplorer64.dec]
hh rom split      n64-gs-v2.0.bin       [-o n64-gs-v2.0.part] [--clean]
hh rom combine    n64-gs-v2.0.part*.bin [-o n64-gs-v2.0.bin]

# Cheat management
hh cheats dump *.rom [--output-format=auto]
hh cheats copy [--input-format=auto] [--output-format=auto] FROM.bin [-o TO.txt]
hh cheats copy [--input-format=auto] [--output-format=auto] FROM.txt [-o TO.bin] [--clean]

*/

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cli = new Cli();
        cli.Always += (_, @params) => PrintBanner(@params);
        cli.OnInfo += (_, @params) => PrintFileInfo(@params);
        cli.OnEncryptRom += (_, @params) => EncryptRom(@params);
        cli.OnDecryptRom += (_, @params) => DecryptRom(@params);
        cli.OnScrambleRom += (_, @params) => ScrambleRom(@params);
        cli.OnUnscrambleRom += (_, @params) => UnscrambleRom(@params);
        cli.OnDumpCheats += (_, @params) => DumpCheats(@params);
        cli.OnCopyCheats += (_, @params) => CopyCheats(@params);
        return await cli.RootCommand.InvokeAsync(args);
    }

    private static void PrintBanner(CmdParams cmdParams)
    {
        bool isColor = cmdParams.PrintFormatId == PrintFormatId.Color;
        if (cmdParams.HideBanner)
        {
            return;
        }

        // ANSI color ASCII art generated with
        // https://github.com/TheZoraiz/ascii-image-converter
        Console.WriteLine();
        Console.WriteLine(
            isColor
                ? Resources.GAMESHARK_LOGO_ASCII_ART_ANSI_TXT.TrimEnd()
                : Resources.GAMESHARK_LOGO_ASCII_ART_PLAIN_TXT);
        Console.WriteLine(Resources.LIBRESHARK_WORDMARK_ASCII_ART_PLAIN_TXT);
    }

    private static void PrintFileInfo(InfoCmdParams @params)
    {
        foreach (FileInfo romFile in @params.InputFiles)
        {
            AbstractCodec codec = AbstractCodec.ReadFromFile(romFile.FullName, @params.InputCodecId);
            var printer = new TerminalPrinter(codec, @params.PrintFormatId);
            printer.PrintDetails(romFile, @params);
        }
    }

    private class FileTransformParams
    {
        public FileInfo InputFile { get; init; }
        public FileInfo OutputFile { get; init; }
        public AbstractCodec Codec { get; init; }
        public TerminalPrinter Printer { get; init; }

        public FileTransformParams(FileInfo inputFile, FileInfo outputFile, AbstractCodec codec, TerminalPrinter printer)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            Codec = codec;
            Printer = printer;
        }
    }

    private static void TransformOneRomFile(
        string status,
        string fileSuffix,
        RomCmdParams @params,
        Func<FileTransformParams, bool> isSupported,
        Action<FileTransformParams> transform)
    {
        FileInfo inputFile = @params.InputFile!;
        FileInfo outputFile = @params.OutputFile ?? GenerateOutputFile(null, inputFile, fileSuffix);

        var codec = AbstractCodec.ReadFromFile(inputFile.FullName);
        var printer = new TerminalPrinter(codec, @params.PrintFormatId);
        var ftp = new FileTransformParams(inputFile, outputFile, codec, printer);

        if (!isSupported(ftp))
        {
            printer.PrintError($"{codec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting.");
            return;
        }
        if (outputFile.Exists && !@params.OverwriteExistingFiles)
        {
            printer.PrintError($"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check.");
            return;
        }

        printer.PrintRomCommand(status, inputFile, outputFile, () =>
        {
            transform(ftp);
        });
    }

    private static void TransformOneCheatFile(
        string status,
        string fileSuffix,
        RomCmdParams @params,
        Func<FileTransformParams, bool> isSupported,
        Action<FileTransformParams> transform)
    {
        FileInfo inputFile = @params.InputFile!;
        FileInfo outputFile = @params.OutputFile ?? GenerateOutputFile(null, inputFile, fileSuffix);

        var codec = AbstractCodec.ReadFromFile(inputFile.FullName);
        var printer = new TerminalPrinter(codec, @params.PrintFormatId);
        var ftp = new FileTransformParams(inputFile, outputFile, codec, printer);

        if (!isSupported(ftp))
        {
            printer.PrintError($"{codec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting.");
            return;
        }
        if (outputFile.Exists && !@params.OverwriteExistingFiles)
        {
            printer.PrintError($"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check.");
            return;
        }

        printer.PrintRomCommand(status, inputFile, outputFile, () =>
        {
            transform(ftp);
        });
    }

    private static void EncryptRom(RomCmdParams @params)
    {
        TransformOneRomFile(
            "Encrypting ROM file",
            "encrypted",
            @params,
            t => t.Codec.SupportsFileEncryption(),
            t => File.WriteAllBytes(t.OutputFile.FullName, t.Codec.Encrypt())
            );
    }

    private static void DecryptRom(RomCmdParams @params)
    {
        TransformOneRomFile(
            "Decrypting ROM file",
            "decrypted",
            @params,
            t => t.Codec.SupportsFileEncryption(),
            t => File.WriteAllBytes(t.OutputFile.FullName, t.Codec.Decrypt())
        );
    }

    private static void ScrambleRom(RomCmdParams @params)
    {
        TransformOneRomFile(
            "Scrambling ROM file",
            "scrambled",
            @params,
            t => t.Codec.SupportsFileScrambling(),
            t => File.WriteAllBytes(t.OutputFile.FullName, t.Codec.Scramble())
        );
    }

    private static void UnscrambleRom(RomCmdParams @params)
    {
        TransformOneRomFile(
            "Unscrambling ROM file",
            "unscrambled",
            @params,
            t => t.Codec.SupportsFileScrambling(),
            t => File.WriteAllBytes(t.OutputFile.FullName, t.Codec.Unscramble())
        );
    }

    private static void DumpCheats(DumpCheatsCmdParams @params)
    {
        foreach (FileInfo inputFile in @params.InputFiles!)
        {
            FileInfo outputFile = GenerateOutputFile(@params.OutputDir, inputFile, "cheats", "txt");

            TransformOneCheatFile(
                "Dumping cheats",
                "cheats",
                new RomCmdParams()
                {
                    InputFile = inputFile,
                    OutputFile = outputFile,
                    OverwriteExistingFiles = @params.OverwriteExistingFiles,
                    PrintFormatId = @params.PrintFormatId,
                    HideBanner = @params.HideBanner,
                    Clean = @params.Clean,
                },
                t => t.Codec.SupportsCheats(),
                t =>
                {
                    TerminalPrinter printer = t.Printer;
                    AbstractCodec inputCodec = t.Codec;
                    CodecId outputCodecId =
                        @params.OutputFormat == CodecId.Auto
                            ? inputCodec.DefaultCheatOutputCodec
                            : @params.OutputFormat;
                    if (outputCodecId is CodecId.UnspecifiedCodecId or CodecId.UnsupportedCodecId)
                    {
                        throw new InvalidOperationException(
                            $"Output codec {@params.OutputFormat} ({@params.OutputFormat.ToDisplayString()}) " +
                            "does not support writing cheats yet.");
                    }

                    AbstractCodec outputCodec =
                        outputFile.Exists
                            ? AbstractCodec.ReadFromFile(outputFile.FullName)
                            : AbstractCodec.CreateFromId(outputFile.FullName, outputCodecId);

                    outputCodec.Games.RemoveAll(_ => true);
                    outputCodec.Games.AddRange(inputCodec.Games);
                    outputCodec.WriteChangesToBuffer();

                    File.WriteAllBytes(outputFile.FullName, outputCodec.Buffer);
                }
            );
        }
    }

    private static void CopyCheats(RomCmdParams @params)
    {
        // TODO(CheatoBaggins): Fix file extension
        FileInfo inputFile = @params.InputFile!;
        var inputCodec = AbstractCodec.ReadFromFile(inputFile.FullName);
        var printer = new TerminalPrinter(inputCodec, @params.PrintFormatId);

        FileInfo outputFile;
        AbstractCodec outputCodec;

        CodecId outputCodecId = @params.OutputFormat;
        if (outputCodecId == CodecId.Auto)
        {
            outputCodecId = inputCodec.DefaultCheatOutputCodec;
        }

        if (@params.OutputFile == null)
        {
            // What extension?
            outputFile = GenerateOutputFile(null, inputFile, "cheats", ".txt");
            outputCodec = AbstractCodec.CreateFromId(outputFile.FullName, outputCodecId);
        }
        else if (@params.OutputFile.Exists)
        {
            outputFile = @params.OutputFile;
            if (@params.OutputFormat == CodecId.Auto)
            {
                outputCodec = AbstractCodec.ReadFromFile(outputFile.FullName);
                outputCodecId = outputCodec.Metadata.CodecId;
            }
            else
            {
                outputCodec = AbstractCodec.CreateFromId(outputFile.FullName, outputCodecId);
            }
        }
        else
        {
            outputFile = @params.OutputFile;
            outputCodec = AbstractCodec.CreateFromId(outputFile.FullName, outputCodecId);
        }

        printer.PrintCheatsCommand("Copying cheats", inputFile, inputCodec, outputFile, outputCodec, () =>
        {
            if (!inputCodec.SupportsCheats())
            {
                printer.PrintError($"{inputCodec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting.");
                return;
            }
            if (!outputCodec.SupportsCheats())
            {
                printer.PrintError($"{outputCodec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting.");
                return;
            }
            if (inputCodec.Metadata.ConsoleId != outputCodec.Metadata.ConsoleId)
            {
                printer.PrintError($"Input and output formats must both be for the same game console. Aborting.");
                return;
            }
            if (outputFile.Exists && !@params.OverwriteExistingFiles)
            {
                printer.PrintError($"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check.");
                return;
            }
            if (outputCodecId is CodecId.UnspecifiedCodecId or CodecId.UnsupportedCodecId)
            {
                throw new InvalidOperationException(
                    $"Output codec {outputCodecId} ({outputCodecId.ToDisplayString()}) " +
                    "does not support writing cheats yet.");
            }

            outputCodec.Games.RemoveAll(_ => true);
            outputCodec.Games.AddRange(inputCodec.Games);
            outputCodec.WriteChangesToBuffer();

            File.WriteAllBytes(outputFile.FullName, outputCodec.Buffer);
        });
    }

    private static FileInfo GenerateOutputFile(DirectoryInfo? outputDir, FileInfo inputFile, string suffix, string? extension = null)
    {
        string inFileName = inputFile.Name;
        string inFileDirPath = inputFile.DirectoryName!;
        string oldExt = extension ?? inputFile.Extension;

        if (!oldExt.StartsWith("."))
        {
            oldExt = $".{oldExt}";
        }

        string newExt = $".{DateTime.Now.ToFilenameString()}.{suffix}{oldExt}";
        string outFileName;

        Match match = Regex.Match(inFileName, "\\.[0-9tTzZ.-]{4,}\\.\\w+\\.\\w+$");
        if (match.Success)
        {
            outFileName = inFileName.Replace(match.Value, newExt);
        }
        else
        {
            outFileName = Path.ChangeExtension(inFileName, newExt);
        }

        string outFileDirPath = outputDir?.FullName ?? inFileDirPath;

        try
        {
            string testFilePath = Path.Join(outFileDirPath, Path.GetRandomFileName());
            File.WriteAllText(testFilePath, "test");
            File.Delete(testFilePath);
        }
        catch
        {
            outFileDirPath = Directory.CreateTempSubdirectory("hammerhead-").FullName;
        }

        return new FileInfo(Path.Join(outFileDirPath, outFileName));
    }
}
