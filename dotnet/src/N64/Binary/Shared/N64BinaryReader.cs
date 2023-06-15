﻿// bacteriamage.wordpress.com

using System.Text;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class for reading (big-endian) integers and c-style strings from byte buffers.
/// </summary>
class N64BinaryReader
{
    public byte[]? Buffer { get; set; }
    public int Position { get; set; }
    public int BytesRead { get; set; }

    public int Length => Buffer?.Length ?? 0;
    public bool EndReached => Position >= Length;

    public N64BinaryReader(byte[] buffer)
    {
        Buffer = buffer;
    }

    public N64BinaryReader()
    {
    }

    public static N64BinaryReader FromFile(string path)
    {
        return new N64BinaryReader(File.ReadAllBytes(path));
    }

    public static N64BinaryReader FromBytes(byte[] bytes)
    {
        return new N64BinaryReader(bytes);
    }

    public N64BinaryReader Seek(int address)
    {
        Position = address;
        return this;
    }

    public byte[] PeekBytes(int addr, int count)
    {
        Seek(addr);
        return PeekBytes(count);
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

    public byte[] ReadBytes(int addr, int count)
    {
        Seek(addr);
        return ReadBytes(count);
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

    public byte ReadUByte(int addr)
    {
        Seek(addr);
        return ReadUByte(addr);
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

    public sbyte ReadSByte(int addr)
    {
        Seek(addr);
        return ReadSByte(addr);
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

    public string ReadCString(int addr, int maxLen = 0)
    {
        Seek(addr);

        StringBuilder builder = new StringBuilder();

        while (NextCharacter(out string character) && (maxLen < 1 || builder.Length < maxLen))
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

    public string ReadPrintableCString(int addr, int maxLen = 0)
    {
        Seek(addr);

        StringBuilder builder = new StringBuilder();

        while (NextPrintableCharacter(out string character) && (maxLen < 1 || builder.Length < maxLen))
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
