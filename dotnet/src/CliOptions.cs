using CommandLine;
using CommandLine.Text;

namespace LibreShark.Hammerhead;

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

// ReSharper disable InconsistentNaming
public enum ReportFormat
{
    unspecified = 0,
    auto,
    color,
    plain,
}
// ReSharper enable InconsistentNaming

// ReSharper disable InconsistentNaming
public enum FileFormat
{
    unspecified = 0,
    auto,
    rom,
    n64_cheats_datel_text,
    n64_cheats_datel_memcard,
    n64_cheats_ed_x7_text,
    n64_cheats_xp_fcd_text,
    n64_cheats_pj_v1_6_text,
    n64_cheats_pj_v3_0_text,
    n64_cheats_openemu_text,
    n64_cheats_cmgsccc_2000_text,
}
// ReSharper enable InconsistentNaming

internal abstract class Options
{
    [Option('C', "clean",
        HelpText = "Try to reset user preferences and active game index, delete invalid cheats, sort game list, " +
                   "etc. By default, no cleaning is performed.")]
    public bool Clean { get; set; }

    [Option('B', "hide-banner",
        HelpText = "Hide the decorative ASCII art banner.")]
    public bool HideBanner { get; set; }
}

[Verb("info",
    HelpText = "Display detailed information about ROM files and cheat list files.")]
internal class InfoOptions : Options
{
    [Option('i', "input-files", Required = true,
        HelpText = "List of video game enhancer firmware dumps (ROM files) and/or cheat list files to analyze.")]
    public IEnumerable<string>? InputFiles { get; set; }

    [Option("input-formats",
        HelpText = "List of file format IDs corresponding to --input-files values. See the FILE_FORMAT section.")]
    public IEnumerable<FileFormat>? InputFormats { get; set; }

    [Option("output-format",
        HelpText = "Report format ID to print output with. See the REPORT_FORMAT section.")]
    public ReportFormat? ReportFormat { get; set; }

    [Usage(ApplicationAlias = "hammerhead")]
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example("Basic usage", new InfoOptions()
            {
                InputFiles = new []{"datel-cheats.txt", "ar3.enc", "n64-gs-v2.0.bin"},
                InputFormats = new []{FileFormat.n64_cheats_datel_text, FileFormat.auto},
                ReportFormat = Hammerhead.ReportFormat.auto,
            });
            // yield return new Example("Logging warnings", UnParserSettings.WithGroupSwitchesOnly(), new Options { InputFile = "file.bin", LogWarning = true });
            // yield return new Example("Logging errors", new[] { UnParserSettings.WithGroupSwitchesOnly(), UnParserSettings.WithUseEqualTokenOnly() }, new Options { InputFile = "file.bin", LogError = true });
        }
    }
}

[Verb("write-rom",
    HelpText = "Reads a ROM file, processes it, and writes the transformed output to disk.")]
internal class WriteRomOptions : Options
{
    [Option('i', "input-files", Required = true,
        HelpText = "One or more paths to ROM files to read.")]
    public IEnumerable<string>? InputFiles { get; set; }

    [Option('o', "output-files", Required = true,
        HelpText = "One or more paths to ROM files to write.")]
    public IEnumerable<string>? OutputFiles { get; set; }

    [Option('y', "overwrite",
        HelpText = "Batch mode. Overwrite existing files. " +
                   "Bypass all prompts and assume the answer is always 'yes'.")]
    public bool Overwrite { get; set; }
}

[Verb("copy-cheats",
    HelpText = "Copy cheats _from_ any compatible file (cheat list or ROM dump) _to_ any compatible file.")]
internal class CopyCheatsOptions : Options
{
    [Option('i', "input-files", Required = true,
        HelpText = "One or more paths to input files to read (ROM dumps or cheat lists).")]
    public IEnumerable<string>? InputFiles { get; set; }

    [Option('o', "output-files", Required = true,
        HelpText = "One or more paths to output files to write (ROM dumps or cheat lists).")]
    public IEnumerable<string>? OutputFiles { get; set; }

    [Option("input-format",
        HelpText = "Force Hammerhead to use the specified file format when reading input files" +
                   "instead of trying to auto-detect the format.")]
    public IEnumerable<FileFormat>? ForceInputFormats { get; set; }

    [Option("output-format",
        HelpText = "Force Hammerhead to use the specified file format when writing output files" +
                   "instead of trying to auto-detect the format.")]
    public IEnumerable<FileFormat>? ForceOutputFormats { get; set; }

    [Option('y', "overwrite",
        HelpText = "Batch mode. Overwrite existing files. " +
                   "Bypass all prompts and assume the answer is always 'yes'.")]
    public bool Overwrite { get; set; }
}
