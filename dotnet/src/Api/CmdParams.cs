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

public class SplitRomCmdParams : CmdParams
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
