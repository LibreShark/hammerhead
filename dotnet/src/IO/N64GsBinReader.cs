// bacteriamage.wordpress.com

using System.Text;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class for reading (big-endian) integers and c-style strings from byte buffers.
/// </summary>
class N64GsBinReader
{
    public readonly byte[] Buffer;

    public int Position { get; set; }

    public int Length => Buffer.Length;
    public bool EndReached => Position >= Length;

    public N64GsBinReader(byte[] buffer)
    {
        Buffer = buffer;
    }

    public N64GsBinReader Seek(int addr)
    {
        Position = addr;
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
        byte[] bytes = PeekBytes(count);
        Position += count;
        return bytes;
    }

    public byte ReadUByte(int addr)
    {
        Seek(addr);
        return ReadUByte();
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

        return Buffer[Position++];
    }

    public sbyte ReadSByte(int addr)
    {
        Seek(addr);
        return ReadSByte();
    }

    public sbyte ReadSByte()
    {
        return (sbyte)ReadUByte();
    }

    public UInt16 ReadUInt16(int addr)
    {
        Seek(addr);
        return ReadUInt16();
    }

    public UInt16 ReadUInt16()
    {
        byte b1 = ReadUByte();
        byte b2 = ReadUByte();
        int value = (b1 << 8) + b2;
        return (UInt16) value;
    }

    public Int16 ReadSInt16(int addr)
    {
        Seek(addr);
        return ReadSInt16();
    }

    public Int16 ReadSInt16()
    {
        return (short)ReadUInt16();
    }

    public UInt32 ReadUInt32(int addr)
    {
        Seek(addr);
        return ReadUInt32();
    }

    public UInt32 ReadUInt32()
    {
        uint high = ReadUInt16();
        uint low = ReadUInt16();

        return (high << 16) + low;
    }

    public Int32 ReadSInt32(int addr)
    {
        Seek(addr);
        return ReadSInt32();
    }

    public Int32 ReadSInt32()
    {
        return (int)ReadUInt32();
    }

    public RomString ReadCString(int maxLen = 0)
    {
        return ReadCString((out string ch) => NextCharacter(out ch), maxLen);
    }

    public RomString ReadCStringAt(int addr, int maxLen = 0)
    {
        Seek(addr);
        return ReadCString((out string ch) => NextCharacter(out ch), maxLen);
    }

    public RomString ReadPrintableCString(int maxLen = 0)
    {
        return ReadCString((out string ch) => NextPrintableCharacter(out ch), maxLen);
    }

    public RomString ReadPrintableCStringAt(int addr, int maxLen = 0)
    {
        Seek(addr);
        return ReadCString((out string ch) => NextPrintableCharacter(out ch), maxLen);
    }

    private RomString ReadCString(TryReadNextChar read, int maxLen)
    {
        uint startPos = (uint)Position;

        StringBuilder builder = new StringBuilder();

        while (read(out string ch) && (maxLen < 1 || builder.Length < maxLen))
        {
            builder.Append(ch);
        }

        uint endPos = (uint)Position;
        uint len = endPos - startPos;

        var romString = new RomString();
        romString.Addr = new RomRange
        {
            StartIndex = startPos,
            EndIndex = endPos,
            Length = len,
        };
        romString.Value = builder.ToString();
        return romString;
    }

    private delegate bool TryReadNextChar(out string ch);

    private bool NextCharacter(out string character)
    {
        if (NextRawChar(out byte b))
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

    private bool NextPrintableCharacter(out string character)
    {
        if (NextRawChar(out byte b) && b >= ' ' && b <= '~')
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

    private bool NextRawChar(out byte b)
    {
        b = ReadUByte();
        return b != 0;
    }
}
