// bacteriamage.wordpress.com

using System.Text;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class for reading (big-endian) integers and c-style strings from byte buffers.
/// </summary>
class BinaryReader
{
    public byte[]? Buffer { get; set; }
    public int Position { get; set; }
    public int BytesRead { get; set; }

    public int Length => Buffer?.Length ?? 0;
    public bool EndReached => Position >= Length;

    public BinaryReader(byte[] buffer)
    {
        Buffer = buffer;
    }

    public BinaryReader()
    {
    }

    public static BinaryReader FromFile(string path)
    {
        return new BinaryReader(File.ReadAllBytes(path));
    }

    public static BinaryReader FromBytes(byte[] bytes)
    {
        return new BinaryReader(bytes);
    }

    public BinaryReader Seek(int address)
    {
        Position = address;
        return this;
    }

    public byte[] PeekBytes(int count)
    {
        if (Buffer == null || Position == Buffer.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {Buffer?.Length} (0x{Buffer?.Length:X8})");
        }
        if (Position < 0 || (Position + count) > Buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {Position+count} (0x{Position+count:X8}). Must be between 0 and {Buffer.Length} (0x{Buffer.Length:X8}).");
        }
        return Buffer.Skip(Position).Take(count).ToArray();
    }

    public byte[] ReadBytes(int count)
    {
        if (Buffer == null || Position == Buffer.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {Buffer?.Length} (0x{Buffer?.Length:X8})");
        }
        if (Position < 0 || (Position + count) > Buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {Position+count} (0x{Position+count:X8}). Must be between 0 and {Buffer.Length} (0x{Buffer.Length:X8}).");
        }
        byte[] bytes = Buffer.Skip(Position).Take(count).ToArray();
        Position += count;
        return bytes;
    }

    public byte ReadUByte()
    {
        if (Buffer == null || Position == Buffer.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {Buffer?.Length} (0x{Buffer?.Length:X8})");
        }
        if (Position < 0 || Position > Buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {Position} (0x{Position:X8}). Must be between 0 and {Buffer.Length} (0x{Buffer.Length:X8}).");
        }

        byte b = Buffer[Position++];

        BytesRead++;

        return b;
    }

    public sbyte ReadSByte()
    {
        return (sbyte)ReadUByte();
    }

    public UInt16 ReadUInt16()
    {
        byte b1 = ReadUByte();
        byte b2 = ReadUByte();
        int value = (b1 << 8) + b2;
        return (UInt16) value;
    }

    public Int16 ReadSInt16()
    {
        return (short)ReadUInt16();
    }

    public UInt32 ReadUInt32()
    {
        uint high = ReadUInt16();
        uint low = ReadUInt16();

        return (high << 16) + low;
    }

    public Int32 ReadSInt32()
    {
        return (int)ReadUInt32();
    }

    public string ReadCString(int max = 0)
    {
        StringBuilder builder = new StringBuilder();

        while (NextCharacter(out string character) && (max < 1 || builder.Length < max))
        {
            builder.Append(character);
        }

        return builder.ToString();
    }

    private bool NextCharacter(out string character)
    {
        if (NextByte(out byte b))
        {
            character = ByteToCharacter(b);
            return true;
        }
        else
        {
            character = "";
            return false;
        }
    }

    public string ReadPrintableCString(int max = 0)
    {
        StringBuilder builder = new StringBuilder();

        while (NextPrintableCharacter(out string character) && (max < 1 || builder.Length < max))
        {
            builder.Append(character);
        }

        return builder.ToString();
    }

    private bool NextPrintableCharacter(out string character)
    {
        if (NextByte(out byte b) && b >= ' ' && b <= '~')
        {
            character = ByteToCharacter(b);
            return true;
        }
        else
        {
            character = "";
            return false;
        }
    }

    private string ByteToCharacter(byte b)
    {
        if (b > 127)
        {
            return string.Concat('`', b.ToString("X2"), '`');
        }
        else
        {
            return Encoding.ASCII.GetString(new byte[] { b });
        }
    }

    private bool NextByte(out byte b)
    {
        b = ReadUByte();
        return b != 0;
    }
}
