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
            var codec = AbstractCodec.FromFile(romFile.FullName);
            var printer = new TerminalPrinter(codec, @params.PrintFormatId);
            printer.PrintDetails(romFile, @params);
        }
    }

    private static void EncryptRom(RomCmdParams @params)
    {
        FileInfo inputFile = @params.InputFile!;
        var codec = AbstractCodec.FromFile(inputFile.FullName);
        var printer = new TerminalPrinter(codec, @params.PrintFormatId);
        if (!codec.FormatSupportsFileEncryption())
        {
            printer.PrintError($"{codec.Metadata.CodecId.ToDisplayString()} ROM files do not support encryption. Aborting.");
            return;
        }
        FileInfo outputFile = @params.OutputFile ?? GenerateOutputFileName(inputFile, "encrypted");
        if (outputFile.Exists && !@params.OverwriteExistingFiles)
        {
            printer.PrintError($"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check.");
            return;
        }
        printer.PrintRomCommand("Encrypting ROM file", inputFile, outputFile, () =>
        {
            File.WriteAllBytes(outputFile.FullName, codec.Encrypt());
        });
    }

    private static void DecryptRom(RomCmdParams @params)
    {
        FileInfo inputFile = @params.InputFile!;
        var codec = AbstractCodec.FromFile(inputFile.FullName);
        var printer = new TerminalPrinter(codec, @params.PrintFormatId);
        if (!codec.FormatSupportsFileEncryption())
        {
            printer.PrintError($"{codec.Metadata.CodecId.ToDisplayString()} ROM files do not support encryption. Aborting.");
            return;
        }
        FileInfo outputFile = @params.OutputFile ?? GenerateOutputFileName(inputFile, "decrypted");
        if (outputFile.Exists && !@params.OverwriteExistingFiles)
        {
            printer.PrintError($"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check.");
            return;
        }
        printer.PrintRomCommand("Decrypting ROM file", inputFile, outputFile, () =>
        {
            File.WriteAllBytes(outputFile.FullName, codec.Buffer);
        });
    }

    private static void ScrambleRom(RomCmdParams @params)
    {
        FileInfo inputFile = @params.InputFile!;
        var codec = AbstractCodec.FromFile(inputFile.FullName);
        var printer = new TerminalPrinter(codec, @params.PrintFormatId);
        if (!codec.FormatSupportsFileScrambling())
        {
            printer.PrintError($"{codec.Metadata.CodecId.ToDisplayString()} ROM files do not support scrambling. Aborting.");
            return;
        }
        FileInfo outputFile = @params.OutputFile ?? GenerateOutputFileName(inputFile, "scrambled");
        if (outputFile.Exists && !@params.OverwriteExistingFiles)
        {
            printer.PrintError($"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check.");
            return;
        }

        printer.PrintRomCommand("Scrambling ROM file", inputFile, outputFile, () =>
        {
            File.WriteAllBytes(outputFile.FullName, codec.Scramble());
        });
    }

    private static void UnscrambleRom(RomCmdParams @params)
    {
        FileInfo inputFile = @params.InputFile!;
        var codec = AbstractCodec.FromFile(inputFile.FullName);
        var printer = new TerminalPrinter(codec, @params.PrintFormatId);
        if (!codec.FormatSupportsFileScrambling())
        {
            printer.PrintError($"{codec.Metadata.CodecId.ToDisplayString()} ROM files do not support scrambling. Aborting.");
            return;
        }
        FileInfo outputFile = @params.OutputFile ?? GenerateOutputFileName(inputFile, "unscrambled");
        if (outputFile.Exists && !@params.OverwriteExistingFiles)
        {
            printer.PrintError($"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check.");
            return;
        }
        printer.PrintRomCommand("Unscrambling ROM file", inputFile, outputFile, () =>
        {
            File.WriteAllBytes(outputFile.FullName, codec.Buffer);
        });
    }

    private static void DumpCheats(DumpCheatsCmdParams @params)
    {
        foreach (FileInfo inputFile in @params.InputFiles)
        {
            var codec = AbstractCodec.FromFile(inputFile.FullName);
            var printer = new TerminalPrinter(codec, @params.PrintFormatId);

            List<Game> games = codec.Games;

            // TODO(CheatoBaggins): Auto-detect output file type!

            FileInfo outputFile = GenerateOutputFileName(inputFile, "cheats");
            if (outputFile.Exists && !@params.OverwriteExistingFiles)
            {
                printer.PrintError($"Output file '{outputFile.FullName}' already exists! Pass --overwrite to bypass this check.");
                continue;
            }
            printer.PrintRomCommand("Dumping cheat file", inputFile, outputFile, () =>
            {
                // File.WriteAllBytes(outputFile.FullName, ...);
            });
        }
    }

    private static FileInfo GenerateOutputFileName(FileInfo inputFile, string suffix)
    {
        string inFileName = inputFile.Name;
        string inFileDir = inputFile.DirectoryName!;
        string oldExt = inputFile.Extension;

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

        string outFileDir = inFileDir;

        try
        {
            string testFilePath = Path.Join(inFileDir, Path.GetRandomFileName());
            File.WriteAllText(testFilePath, "test");
            File.Delete(testFilePath);
        }
        catch
        {
            outFileDir = Directory.CreateTempSubdirectory("hammerhead-").FullName;
        }

        return new FileInfo(Path.Join(outFileDir, outFileName));
    }
}
