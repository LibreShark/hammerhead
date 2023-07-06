using System.Reflection;

namespace LibreShark.Hammerhead.Api;

public class DumpFactory
{
    public HammerheadDump Dumpify(IEnumerable<ParsedFile> parsedFiles)
    {
        var entryAssembly = Assembly.GetEntryAssembly()!;
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
        return dump;
    }
}
