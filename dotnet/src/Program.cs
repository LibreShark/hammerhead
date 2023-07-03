using System.CommandLine;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Google.Protobuf.Collections;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead;

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

    private static void PrintBanner(CmdParams @params)
    {
        if (@params.PrintFormatId is PrintFormatId.Json)
        {
            return;
        }
        var printer = new TerminalPrinter(printFormat: @params.PrintFormatId);
        printer.PrintBanner(@params);
    }

    private static T Try<T>(FileInfo inputFile, Func<T> factory)
    {
        T value;

        try
        {
            value = factory.Invoke();
        }
        catch
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine();
            Console.Error.WriteLine($"ERROR while reading file '{inputFile.FullName}'!");
            Console.Error.WriteLine();
            Console.Error.WriteLine();
            throw;
        }

        return value;
    }

    private static void PrintFileInfo(InfoCmdParams @params)
    {
        var parsedFiles = new RepeatedField<ParsedFile>();

        foreach (FileInfo inputFile in @params.InputFiles)
        {

            AbstractCodec codec = Try(inputFile, () => AbstractCodec.ReadFromFile(inputFile.FullName, @params.InputCodecId));

            if (@params.PrintFormatId is PrintFormatId.Json)
            {
                parsedFiles.Add(codec.ToSlimProto());
            }
            else
            {
                var printer = new TerminalPrinter(codec, @params.PrintFormatId);
                printer.PrintFileInfo(inputFile, @params);
            }
        }

        if (@params.PrintFormatId is PrintFormatId.Json)
        {
            var entryAssembly = Assembly.GetEntryAssembly()!;
            // TODO(CheatoBaggins): De-duplicate with ProtobufJson
            var dump = new HammerheadDump()
            {
                AppInfo = new AppInfo()
                {
                    AppName = entryAssembly.GetName().Name,
                    SemanticVersion = GitVersionInformation.AssemblySemVer,
                    InformationalVersion = GitVersionInformation.InformationalVersion,
                    BuildDateIso = entryAssembly.GetBuildDate().ToIsoString(),
                    WriteDateIso = DateTimeOffset.Now.ToIsoString(),
                    SourceCodeUrl = "https://github.com/LibreShark/hammerhead",
                },
            };
            dump.ParsedFiles.AddRange(parsedFiles);
            // TODO(CheatoBaggins): De-duplicate with ProtobufJson
            var formatter = new JsonFormatter(
                JsonFormatter.Settings.Default
                    .WithIndentation()
                    .WithFormatDefaultValues(true)
                    .WithPreserveProtoFieldNames(true)
            );
            Console.WriteLine(formatter.Format(dump) + "\n");
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

        var printer = new TerminalPrinter(@params.PrintFormatId);
        printer.PrintRomCommand(status, inputFile, outputFile, () =>
        {
            AbstractCodec codec = Try(inputFile, () => AbstractCodec.ReadFromFile(inputFile.FullName));
            var fileParams = new FileTransformParams(inputFile, outputFile, codec, printer);

            if (!isSupported(fileParams))
            {
                printer.PrintError(new InvalidOperationException(
                    $"{codec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting."
                ));
                return;
            }

            if (outputFile.Exists && !@params.OverwriteExistingFiles)
            {
                printer.PrintError(new InvalidOperationException(
                    $"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check."
                ));
                return;
            }

            transform(fileParams);
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
            CopyCheats(new RomCmdParams()
            {
                InputFile = inputFile,
                PrintFormatId = @params.PrintFormatId,
                OutputFormat = @params.OutputFormat,
                OverwriteExistingFiles = @params.OverwriteExistingFiles,
                Clean = @params.Clean,
                HideBanner = @params.HideBanner,
            });
        }
    }

    private static void CopyCheats(RomCmdParams @params)
    {
        FileInfo inputFile = @params.InputFile!;
        AbstractCodec inputCodec = Try(inputFile, () => AbstractCodec.ReadFromFile(inputFile.FullName));
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
            string extension = outputCodecId.FileExtension();
            outputFile = GenerateOutputFile(null, inputFile, "cheats", extension);
            outputCodec = AbstractCodec.CreateFromId(outputFile.FullName, outputCodecId);
        }
        else if (@params.OutputFile.Exists)
        {
            outputFile = @params.OutputFile;
            if (@params.OutputFormat == CodecId.Auto)
            {
                outputCodec = Try(outputFile, () => AbstractCodec.ReadFromFile(outputFile.FullName));
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
                printer.PrintError(new InvalidOperationException(
                    $"{inputCodec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting."
                ));
                return;
            }
            if (!outputCodec.SupportsCheats())
            {
                printer.PrintError(new InvalidOperationException(
                    $"{outputCodec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting."
                ));
                return;
            }
            if (outputCodec.Metadata.ConsoleId != inputCodec.Metadata.ConsoleId &&
                outputCodec.Metadata.ConsoleId != ConsoleId.Universal)
            {
                printer.PrintError(new InvalidOperationException(
                    $"Input and output formats ({inputCodec.Metadata.ConsoleId} and {outputCodec.Metadata.ConsoleId}) must both be for the same game console. Aborting."
                ));
                return;
            }
            if (outputFile.Exists && !@params.OverwriteExistingFiles)
            {
                printer.PrintError(new InvalidOperationException(
                    $"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check."
                ));
                return;
            }
            if (outputCodecId is CodecId.UnspecifiedCodecId or CodecId.UnsupportedCodecId)
            {
                printer.PrintError(new InvalidOperationException(
                    $"Output codec {outputCodecId} ({outputCodecId.ToDisplayString()}) " +
                    "does not support writing cheats yet."
                ));
                return;
            }

            outputCodec.ImportFromProto(inputCodec.ToSlimProto());

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
