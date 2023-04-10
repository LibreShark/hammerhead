// bacteriamage.wordpress.com

using System.Text;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Write a single game's cheat list to a controller-pak note file in MPK format.
/// </summary>
class NoteWriter
{
    public BinaryWriter Writer { get; set; }

    public static void ToFile(Game game, string path)
    {
        NoteWriter writer = new NoteWriter();
        writer.WriteGameNote(game);
        File.WriteAllBytes(path, writer.Writer.Buffer ?? Array.Empty<byte>());
    }

    public NoteWriter()
    {
        Writer = new BinaryWriter(16);
    }

    public void WriteGameNote(Game game)
    {
        WriteMpkEditHeader();
        WriteMpkComment(game.Name);
        WriteNoteEntry();
        WriteGame(game);
    }

    private void WriteMpkEditHeader()
    {
        Writer.WriteByte(0x01);
        Writer.WriteCString("MPKNote");
        Writer.WriteInt16(0x0000);       // unused
        Writer.WriteUInt32(0x00000000);  // timestamp
        Writer.WriteByte(0x00);          // length of comment
    }

    private void WriteMpkComment(string comment)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(comment.Replace(Environment.NewLine, "\n"));

        if (bytes.Length > 0)
        {
            Writer.AutoExtendSize = 16;
            Writer.BytesWritten = 0;

            foreach (byte b in bytes)
            {
                Writer.WriteByte(b);
            }

            while (Writer.BytesWritten % 16 != 0)
            {
                Writer.WriteByte(0);
            }

            Writer.Seek(15);
            Writer.WriteByte(Writer.BytesWritten / 16);
            Writer.SeekEnd();
        }

    }

    private void WriteNoteEntry()
    {
        Writer.WriteUInt32(0x3BADD1E5); // Game Code        ; "3 Baddies"?
        Writer.WriteInt16(0xFADE);      // Publisher Code   ; "Fade"
        Writer.WriteInt16(0xCAFE);      // Start page (MPKEdit dummy value)
        Writer.WriteByte(0x03);         // Status bitfield
        Writer.WriteByte(0x00);         // Reserved; unused
        Writer.WriteInt16(0x0000);      // "Data sum"; always zero
        Writer.WriteUInt32(0x00000000); // File extension

        // File name
        Writer.WriteBytes(new byte[]
        {
            // "GAME-SAVEDAT"
            0x20, 0x1A, 0x26, 0x1E, 0x3B, 0x2C, 0x1A, 0x2F,
            0x1E, 0x1D, 0x1A, 0x2D, 0x00, 0x00, 0x00, 0x00,
        });
    }

    private void WriteGame(Game game)
    {
        Writer.SeekEnd();
        Writer.AutoExtendSize = 256;

        Writer.WriteUInt32(0x4E363400); // Magic number: "N64\0"

        Writer.WriteUInt32(0);          // Length of note
        Writer.BytesWritten = 0;

        EncodeGame(game);

        Writer.SeekOffset(-Writer.BytesWritten - 4);
        Writer.WriteSInt32(Writer.BytesWritten);
        Writer.SeekEnd();
    }

    private void EncodeGame(Game game)
    {
        GameEncoder encoder = new GameEncoder(Writer);
        encoder.EncodeGame(game);
    }
}
