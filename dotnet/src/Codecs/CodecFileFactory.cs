namespace LibreShark.Hammerhead.Codecs;

public class CodecFileFactory
{
    public Func<Byte[], bool> AutoDetect { get; }
    public Func<CodecId, bool> IsCodec { get; }
    public Func<string, Byte[], AbstractCodec> Create { get; }

    public CodecFileFactory(
        Func<Byte[], bool> autoDetect,
        Func<CodecId, bool> isCodec,
        Func<string, Byte[], AbstractCodec> create
    )
    {
        AutoDetect = autoDetect;
        IsCodec = isCodec;
        Create = create;
    }
}