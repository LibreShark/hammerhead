﻿// bacteriamage.wordpress.com

using System.Text;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class for reading (big-endian) integers and c-style strings from byte buffers.
/// </summary>
class N64GsBinReader
{
    private readonly byte[] _buffer;

    public uint Position;

    public N64GsBinReader(byte[] buffer)
    {
        _buffer = buffer;
    }

    public bool EndReached => Position >= _buffer.Length;

    public int Find(string needle)
    {
        return _buffer.Find(needle);
    }

    public int Find(byte[] needle)
    {
        return _buffer.Find(needle);
    }

    public bool Contains(string needle)
    {
        return _buffer.Contains(needle);
    }

    public bool Contains(byte[] needle)
    {
        return _buffer.Contains(needle);
    }

    public N64GsBinReader Seek(uint addr)
    {
        Position = addr;
        return this;
    }

    public byte[] PeekBytesAt(uint addr, uint count)
    {
        if (addr == _buffer.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {_buffer.Length} (0x{_buffer.Length:X8})");
        }
        if (addr + count > _buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {addr+count} (0x{addr+count:X8}). Must be between 0 and {_buffer.Length} (0x{_buffer.Length:X8}).");
        }
        return _buffer.Skip((int)addr).Take((int)count).ToArray();
    }

    public byte[] PeekBytes(uint count)
    {
        return PeekBytesAt(Position, count);
    }

    public byte[] ReadBytesAt(uint addr, uint count)
    {
        Seek(addr);
        return ReadBytes(count);
    }

    public byte[] ReadBytes(uint count)
    {
        byte[] bytes = PeekBytes(count);
        Position += count;
        return bytes;
    }

    public byte ReadUByte(uint addr)
    {
        Seek(addr);
        return ReadUByte();
    }

    public byte ReadUByte()
    {
        if (Position == _buffer.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {_buffer.Length} (0x{_buffer.Length:X8})");
        }
        if (Position > _buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {Position} (0x{Position:X8}). Must be between 0 and {_buffer.Length} (0x{_buffer.Length:X8}).");
        }

        return _buffer[Position++];
    }

    public sbyte ReadSByte(uint addr)
    {
        Seek(addr);
        return ReadSByte();
    }

    public sbyte ReadSByte()
    {
        return (sbyte)ReadUByte();
    }

    public UInt16 ReadUInt16(uint addr)
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

    public Int16 ReadSInt16(uint addr)
    {
        Seek(addr);
        return ReadSInt16();
    }

    public Int16 ReadSInt16()
    {
        return (short)ReadUInt16();
    }

    public UInt32 ReadUInt32(uint addr)
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

    public Int32 ReadSInt32(uint addr)
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

    public RomString ReadCStringAt(uint addr, int maxLen = 0)
    {
        Seek(addr);
        return ReadCString((out string ch) => NextCharacter(out ch), maxLen);
    }

    public RomString ReadPrintableCString(int maxLen = 0)
    {
        return ReadCString((out string ch) => NextPrintableCharacter(out ch), maxLen);
    }

    public RomString ReadPrintableCStringAt(uint addr, int maxLen = 0)
    {
        Seek(addr);
        return ReadCString((out string ch) => NextPrintableCharacter(out ch), maxLen);
    }

    private RomString ReadCString(TryReadNextChar read, int maxLen)
    {
        uint startPos = Position;

        StringBuilder builder = new StringBuilder();

        while (read(out string ch) && (maxLen < 1 || builder.Length < maxLen))
        {
            builder.Append(ch);
        }

        uint endPos = Position;
        uint len = endPos - startPos;

        return new RomString
        {
            Addr = new RomRange
            {
                StartIndex = startPos,
                EndIndex = endPos,
                Length = len,
            },
            Value = builder.ToString(),
        };
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
