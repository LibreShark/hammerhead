using LibreShark.Hammerhead.Codecs;

namespace LibreShark.Hammerhead.Cli;

internal class FileTransformParams
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
