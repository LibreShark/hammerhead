using LibreShark.Hammerhead.Nintendo64;

namespace LibreShark.Hammerhead.Api;

public abstract class CmdParams : EventArgs
{
    public PrintFormatId PrintFormatId { get; set; }
    public bool HideBanner { get; init; }
    public bool Clean { get; init; }
}

public class InfoCmdParams : CmdParams
{
    public CodecId InputCodecId { get; init; }
    public bool HideGames { get; init; }
    public bool HideCheats { get; init; }
    public bool HideCodes { get; init; }
    public FileInfo[] InputFiles { get; init; }

    public InfoCmdParams()
    {
        InputFiles = new FileInfo[] { };
    }
}

public class RomCmdParams : CmdParams
{
    public FileInfo? InputFile { get; init; }
    public FileInfo? OutputFile { get; set; }
    public bool OverwriteExistingFiles { get; init; }
    public CodecId OutputFormat { get; init; }
}

public class ExtractRomCmdParams : CmdParams
{
    public FileInfo[]? InputFiles { get; init; }
    public DirectoryInfo? OutputDir { get; set; }
    public bool OverwriteExistingFiles { get; init; }
}

public class DumpCheatsCmdParams : CmdParams
{
    public FileInfo[]? InputFiles { get; init; }
    public DirectoryInfo? OutputDir { get; set; }
    public bool OverwriteExistingFiles { get; init; }
    public CodecId OutputFormat { get; init; }
}

public class N64GsConfigureCmdParams : CmdParams
{
    public FileInfo? InputFile { get; init; }
    public FileInfo? OutputFile { get; set; }
    public bool OverwriteExistingFiles { get; init; }
    public N64KeyCodeId[]? KeyCodeIds { get; init; }
    public string? SelectedGame { get; init; }
    public bool? IsSoundEnabled { get; init; }
    public bool? IsMenuScrollEnabled { get; init; }
    public bool? IsBgScrollEnabled { get; init; }
    public Nn64GsBgPatternId? BgPattern { get; init; }
    public Nn64GsBgColorId? BgColor { get; init; }
    public bool? UpdateTimestamp { get; init; }
    public bool? RenameKeyCodes { get; init; }
    public bool? ResetUserPrefs { get; init; }
}
