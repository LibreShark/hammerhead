// bacteriamage.wordpress.com

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Read a single game's cheat list from a controller-pak note file in MPK format.
/// </summary>
class NoteReader
{
    public BinaryReader Reader { get; set; }

    public static N64Game FromFile(string path)
    {
        return new NoteReader(BinaryReader.FromFile(path)).ReadGame();
    }

    public NoteReader(BinaryReader reader)
    {
        Reader = reader;
    }

    public N64Game ReadGame()
    {
        SkipMpkHeader();
        ValidateMagicNumber();
        return DecodeGame();
    }

    private void SkipMpkHeader()
    {
        if (Reader.ReadUByte() != 0x01 || string.Compare(Reader.ReadCString(Reader.Position), "MPKNote", StringComparison.InvariantCulture) != 0)
        {
            throw new Exception("Not an MPK Note");
        }

        Reader.ReadUInt16();  // unused
        Reader.ReadUInt32();  // timestamp

        int commentBytes = Reader.ReadUByte() * 16;
        int noteEntryBytes = 32;

        Reader.Position += (commentBytes + noteEntryBytes);
    }

    private void ValidateMagicNumber()
    {
        if (Reader.ReadUInt32() != 0x4E363400) // N64\0
        {
            throw new Exception("Invalid note header");
        }
    }

    private N64Game DecodeGame()
    {
        N64Game game;

        Reader.BytesRead = Reader.ReadSInt32() * -1;

        game = GameDecoder.FromReader(Reader);

        if (Reader.BytesRead != 0)
        {
            throw new Exception("Invalid note length");
        }

        return game;
    }
}
