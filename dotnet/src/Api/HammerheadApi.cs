using LibreShark.Hammerhead.Cli;
using LibreShark.Hammerhead.Codecs;

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
