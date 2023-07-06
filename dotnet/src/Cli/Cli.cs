﻿using System.CommandLine;
using System.Reflection;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Google.Protobuf.Collections;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Codecs;

namespace LibreShark.Hammerhead.Cli;

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

public class HammerheadCli
{
    private readonly string[] _args;
    private readonly HammerheadApi _api;
    private ICliPrinter _printer = new TerminalPrinter();

    public HammerheadCli(string[] args)
    {
        _args = args;
        _api = new HammerheadApi();
    }

    public async Task<int> InvokeAsync()
    {
        var cli = new CliCmd();
        cli.Always += (_, @params) => PrintBanner(@params);
        cli.OnInfo += (_, @params) => PrintFileInfo(@params);
        cli.OnEncryptRom += (_, @params) => EncryptRom(@params);
        cli.OnDecryptRom += (_, @params) => DecryptRom(@params);
        cli.OnScrambleRom += (_, @params) => ScrambleRom(@params);
        cli.OnUnscrambleRom += (_, @params) => UnscrambleRom(@params);
        cli.OnDumpCheats += (_, @params) => DumpCheats(@params);
        cli.OnCopyCheats += (_, @params) => CopyCheats(@params);
        return await cli.RootCommand.InvokeAsync(_args);
    }

    private void PrintBanner(CliCmdParams cmdParams)
    {
        _printer = new TerminalPrinter(printFormat: cmdParams.PrintFormatId);
        _printer.PrintBanner(cmdParams);
    }

    private void PrintFileInfo(InfoCmdParams cmdParams)
    {
        if (cmdParams.PrintFormatId is PrintFormatId.Json)
        {
            HammerheadDump dump = _api.GetDump(cmdParams, full: false);
            var formatter = new JsonFormatter(
                JsonFormatter.Settings.Default
                    .WithIndentation()
                    .WithFormatDefaultValues(true)
                    .WithPreserveProtoFieldNames(true)
            );
            _printer.PrintJson(formatter, dump);
        }
        else
        {
            List<ICodec> codecs = _api.ParseFiles(cmdParams);
            foreach (ICodec codec in codecs)
            {
                _printer = new TerminalPrinter(codec, cmdParams.PrintFormatId);
                _printer.PrintFileInfo(codec.Metadata.FilePath, cmdParams);
            }
        }
    }

    private class FileTransformParams
    {
        public FileInfo InputFile { get; init; }
        public FileInfo OutputFile { get; init; }
        public ICodec Codec { get; init; }
        public ICliPrinter Printer { get; init; }

        public FileTransformParams(FileInfo inputFile, FileInfo outputFile, ICodec codec, ICliPrinter printer)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            Codec = codec;
            Printer = printer;
        }
    }

    private void TransformOneRomFile(
        string status,
        string fileSuffix,
        RomCmdParams romParams,
        Func<FileTransformParams, bool> isSupported,
        Func<FileTransformParams, u8[]> transform)
    {
        FileInfo inputFile = romParams.InputFile!;
        FileInfo outputFile = romParams.OutputFile ?? GenerateOutputFile(null, inputFile, fileSuffix);

        _printer = new TerminalPrinter(romParams.PrintFormatId);
        _printer.PrintRomCommand(status, inputFile, outputFile, () =>
        {
            // When cheats are dumped/copied, the input and output codecs may be different, but
            // when a ROM file is "transformed", the input codec and output codec are always the same.
            ICodec codec = Try(inputFile, () => AbstractCodec.ReadFromFile(inputFile.FullName));

            var fileParams = new FileTransformParams(inputFile, outputFile, codec, _printer);

            if (!isSupported(fileParams))
            {
                _printer.PrintError(new InvalidOperationException(
                    $"{codec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting."
                ));
                return;
            }

            if (outputFile.Exists && !romParams.OverwriteExistingFiles)
            {
                _printer.PrintError(new InvalidOperationException(
                    $"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check."
                ));
                return;
            }

            u8[] bytes = transform(fileParams);
            File.WriteAllBytes(outputFile.FullName, bytes);
        });
    }

    private void EncryptRom(RomCmdParams romParams)
    {
        TransformOneRomFile(
            "Encrypting ROM file",
            "encrypted",
            romParams,
            t => t.Codec.SupportsFileEncryption(),
            t => t.Codec.Encrypt()
            );
    }

    private void DecryptRom(RomCmdParams romParams)
    {
        TransformOneRomFile(
            "Decrypting ROM file",
            "decrypted",
            romParams,
            t => t.Codec.SupportsFileEncryption(),
            t => t.Codec.Decrypt()
        );
    }

    private void ScrambleRom(RomCmdParams romParams)
    {
        TransformOneRomFile(
            "Scrambling ROM file",
            "scrambled",
            romParams,
            t => t.Codec.SupportsFileScrambling(),
            t => t.Codec.Scramble()
        );
    }

    private void UnscrambleRom(RomCmdParams romParams)
    {
        TransformOneRomFile(
            "Unscrambling ROM file",
            "unscrambled",
            romParams,
            t => t.Codec.SupportsFileScrambling(),
            t => t.Codec.Unscramble()
        );
    }

    private void DumpCheats(DumpCheatsCmdParams dumpParams)
    {
        foreach (FileInfo inputFile in dumpParams.InputFiles!)
        {
            CopyCheats(new RomCmdParams()
            {
                InputFile = inputFile,
                PrintFormatId = dumpParams.PrintFormatId,
                OutputFormat = dumpParams.OutputFormat,
                OverwriteExistingFiles = dumpParams.OverwriteExistingFiles,
                Clean = dumpParams.Clean,
                HideBanner = dumpParams.HideBanner,
            });
        }
    }

    private void CopyCheats(RomCmdParams romParams)
    {
        FileInfo inputFile = romParams.InputFile!;
        ICodec inputCodec = Try(inputFile, () => AbstractCodec.ReadFromFile(inputFile.FullName));
        _printer = new TerminalPrinter(inputCodec, romParams.PrintFormatId);

        FileInfo outputFile;
        ICodec outputCodec;

        CodecId outputCodecId = romParams.OutputFormat;
        if (outputCodecId == CodecId.Auto)
        {
            outputCodecId = inputCodec.DefaultCheatOutputCodec;
        }

        if (romParams.OutputFile == null)
        {
            string extension = outputCodecId.FileExtension();
            outputFile = GenerateOutputFile(null, inputFile, "cheats", extension);
            outputCodec = AbstractCodec.CreateFromId(outputFile.FullName, outputCodecId);
        }
        else if (romParams.OutputFile.Exists)
        {
            outputFile = romParams.OutputFile;
            if (romParams.OutputFormat == CodecId.Auto)
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
            outputFile = romParams.OutputFile;
            outputCodec = AbstractCodec.CreateFromId(outputFile.FullName, outputCodecId);
        }

        _printer.PrintCheatsCommand("Copying cheats", inputFile, inputCodec, outputFile, outputCodec, () =>
        {
            if (!inputCodec.SupportsCheats())
            {
                _printer.PrintError(new InvalidOperationException(
                    $"{inputCodec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting."
                ));
                return;
            }
            if (!outputCodec.SupportsCheats())
            {
                _printer.PrintError(new InvalidOperationException(
                    $"{outputCodec.Metadata.CodecId.ToDisplayString()} files do not support this operation. Aborting."
                ));
                return;
            }
            if (outputCodec.Metadata.ConsoleId != inputCodec.Metadata.ConsoleId &&
                outputCodec.Metadata.ConsoleId != ConsoleId.Universal)
            {
                _printer.PrintError(new InvalidOperationException(
                    $"Input and output formats ({inputCodec.Metadata.ConsoleId} and {outputCodec.Metadata.ConsoleId}) must both be for the same game console. Aborting."
                ));
                return;
            }
            if (outputFile.Exists && !romParams.OverwriteExistingFiles)
            {
                _printer.PrintError(new InvalidOperationException(
                    $"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check."
                ));
                return;
            }
            if (outputCodecId is CodecId.UnspecifiedCodecId or CodecId.UnsupportedCodecId)
            {
                _printer.PrintError(new InvalidOperationException(
                    $"Output codec {outputCodecId} ({outputCodecId.ToDisplayString()}) " +
                    "does not support writing cheats yet."
                ));
                return;
            }

            outputCodec.ImportFromProto(inputCodec.ToFullProto());

            File.WriteAllBytes(outputFile.FullName, outputCodec.Buffer);
        });
    }

    private FileInfo GenerateOutputFile(DirectoryInfo? outputDir, FileInfo inputFile, string suffix, string? extension = null)
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

    private T Try<T>(FileSystemInfo inputFile, Func<T> valueFactory)
    {
        T value;

        try
        {
            value = valueFactory.Invoke();
        }
        catch
        {
            _printer.PrintError($"ERROR while reading file '{inputFile.FullName}'!");
            throw;
        }

        return value;
    }
}
