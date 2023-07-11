using System.Text.RegularExpressions;
using LibreShark.Hammerhead.Cli;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.Nintendo64;

namespace LibreShark.Hammerhead.Api;

public class HammerheadApi
{
    private ICliPrinter _printer = new TerminalPrinter();

    public List<ICodec> ParseFiles(InfoCmdParams cmdParams)
    {
        var codecs = new List<ICodec>();

        foreach (FileInfo inputFile in cmdParams.InputFiles)
        {
            ICodec codec = Try(inputFile, () => AbstractCodec.ReadFromFile(inputFile.FullName, cmdParams.InputCodecId));
            codecs.Add(codec);
        }

        return codecs;
    }

    public HammerheadDump GetDump(InfoCmdParams cmdParams, bool full = true)
    {
        var codecs = new List<ICodec>();

        foreach (FileInfo inputFile in cmdParams.InputFiles)
        {
            ICodec codec = Try(inputFile, () => AbstractCodec.ReadFromFile(inputFile.FullName, cmdParams.InputCodecId));
            codecs.Add(codec);
        }

        return new DumpFactory().Dumpify(codecs.Select(codec => full ? codec.ToFullProto() : codec.ToSlimProto()));
    }

    private void TransformOneRomFile(
        RomCmdParams romParams,
        Func<FileTransformParams, bool> isSupported,
        Func<FileTransformParams, byte[]> transform,
        FileInfo inputFile,
        FileInfo outputFile)
    {
        _printer = new TerminalPrinter(romParams.PrintFormatId);

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

        // TODO(CheatoBaggins): Return bytes and output filename; do not write file here.
        File.WriteAllBytes(outputFile.FullName, bytes);
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
            TransformOneRomFile(romParams, isSupported, transform, inputFile, outputFile);
        });
    }

    public void EncryptRom(RomCmdParams romParams)
    {
        TransformOneRomFile(
            "Encrypting ROM file",
            "encrypted",
            romParams,
            t => t.Codec.SupportsFileEncryption(),
            t => t.Codec.Encrypt()
        );
    }

    public void DecryptRom(RomCmdParams romParams)
    {
        TransformOneRomFile(
            "Decrypting ROM file",
            "decrypted",
            romParams,
            t => t.Codec.SupportsFileEncryption(),
            t => t.Codec.Decrypt()
        );
    }

    public void ScrambleRom(RomCmdParams romParams)
    {
        TransformOneRomFile(
            "Scrambling ROM file",
            "scrambled",
            romParams,
            t => t.Codec.SupportsFileScrambling(),
            t => t.Codec.Scramble()
        );
    }

    public void UnscrambleRom(RomCmdParams romParams)
    {
        TransformOneRomFile(
            "Unscrambling ROM file",
            "unscrambled",
            romParams,
            t => t.Codec.SupportsFileScrambling(),
            t => t.Codec.Unscramble()
        );
    }

    public void ExtractRom(ExtractRomCmdParams romParams)
    {
        foreach (FileInfo inputFile in romParams.InputFiles!)
        {
            ICodec codec = AbstractCodec.ReadFromFile(inputFile.FullName);
            _printer = new TerminalPrinter(codec, romParams.PrintFormatId);
            foreach (EmbeddedFile embeddedFile in codec.EmbeddedFiles)
            {
                FileInfo outputFile = GetEmbeddedOutputFilePath(inputFile, romParams.OutputDir, embeddedFile.FileName);
                _printer.PrintLine($"Extracting embedded file to: {outputFile.FullName}");
                using FileStream outputFileStream = outputFile.OpenWrite();
                outputFileStream.Write(embeddedFile.UncompressedBytes);
            }
            foreach (EmbeddedImage embeddedImage in codec.EmbeddedImages)
            {
                string embeddedFileName = Path.ChangeExtension(embeddedImage.FileName, ".png");
                FileInfo outputFile = GetEmbeddedOutputFilePath(inputFile, romParams.OutputDir, embeddedFileName);
                _printer.PrintLine($"Extracting embedded file to: {outputFile.FullName}");
                using FileStream outputFileStream = outputFile.OpenWrite();
                embeddedImage.Image.SaveAsPng(outputFileStream);
            }
        }
    }

    private static FileInfo GetEmbeddedOutputFilePath(
        FileInfo inputFile, DirectoryInfo? outputDir, string embeddedFileName)
    {
        string outDirName = Path.GetFileNameWithoutExtension(inputFile.Name);
        FileInfo fakeEmbeddedFile = new FileInfo(Path.Join(inputFile.DirectoryName, outDirName, embeddedFileName));
        FileInfo outputFile = GetOutputFilePath(fakeEmbeddedFile, outputDir, embeddedFileName);
        return outputFile;
    }

    public ICodec[] DumpCheats(DumpCheatsCmdParams dumpParams)
    {
        var outputCodecs = new List<ICodec>();
        foreach (FileInfo inputFile in dumpParams.InputFiles!)
        {
            ICodec outputCodec = CopyCheats(new RomCmdParams()
            {
                InputFile = inputFile,
                PrintFormatId = dumpParams.PrintFormatId,
                OutputFormat = dumpParams.OutputFormat,
                OverwriteExistingFiles = dumpParams.OverwriteExistingFiles,
                Clean = dumpParams.Clean,
                HideBanner = dumpParams.HideBanner,
            });
            outputCodecs.Add(outputCodec);
        }
        return outputCodecs.ToArray();
    }

    public ICodec CopyCheats(RomCmdParams romParams)
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

        return outputCodec;
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

        return GetOutputFilePath(inputFile, outputDir, outFileName);
    }

    private static FileInfo GetOutputFilePath(
        FileInfo inputFile, DirectoryInfo? outputDir, string outFileName)
    {
        string outFileDirPath = outputDir?.FullName ?? inputFile.DirectoryName!;

        try
        {
            Directory.CreateDirectory(outFileDirPath);
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

    public void ReorderKeycodes(ReorderKeycodeCmdParams cmdParams)
    {
        var codec = AbstractCodec.ReadFromFile(cmdParams.InputFile!.FullName);
        if (!codec.Metadata.CodecFeatureSupport.SupportsKeyCodes)
        {
            string codecName = codec.Metadata.CodecId.ToDisplayString();
            _printer.PrintError($"'{codecName}' files do not support key codes!");
            return;
        }

        List<Code> curKeyCodes = codec.ToFullProto().N64Data.KeyCodes.ToList();
        List<N64KeyCodeId> oldKeyCodeIds = curKeyCodes.Select(kc =>
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
        }).ToList();

        N64KeyCodeId[] newKeyCodeIds = cmdParams.KeyCodeIds!;
        int actualCount = newKeyCodeIds.Length;
        int expectedCount = oldKeyCodeIds.Count;
        if (actualCount != expectedCount)
        {
            _printer.PrintError(
                $"This ROM only supports {expectedCount} key codes, but got {actualCount}: " +
                string.Join(", ", newKeyCodeIds) + ".");
            return;
        }

        var oldKeyCodeIdSet = new HashSet<N64KeyCodeId>(oldKeyCodeIds);
        var newKeyCodeIdSet = new HashSet<N64KeyCodeId>(newKeyCodeIds);

        if (!newKeyCodeIdSet.SetEquals(oldKeyCodeIdSet))
        {
            _printer.PrintError(
                $"This ROM supports {expectedCount} distinct key codes " +
                $"({string.Join(", ", oldKeyCodeIds)}), but got {actualCount}: " +
                string.Join(", ", newKeyCodeIds) + ".");
            return;
        }

        var newKeyCodes = new List<Code>();

        // TODO(CheatoBaggins): Implement.
    }
}
