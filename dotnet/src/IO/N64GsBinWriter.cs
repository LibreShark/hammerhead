﻿// bacteriamage.wordpress.com

using System.Globalization;
using System.Text;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class for writing (big-endian) integers and c-style strings to byte buffers.
/// </summary>
class N64GsBinWriter
{
    private readonly byte[] _buffer;

    private uint _position;

    public N64GsBinWriter(int bufferSize)
        : this(new byte[bufferSize])
    {
    }

    public N64GsBinWriter(byte[] buffer)
    {
        _buffer = buffer;
    }

    public void WriteToFile(string path)
    {
        File.WriteAllBytes(path, _buffer);
    }

    public void Seek(uint position)
    {
        _position = position;
    }

    public void SeekEnd()
    {
        Seek((uint)_buffer.Length);
    }

    public void SeekOffset(uint offset)
    {
        offset += _position;

        if (offset >= _buffer.Length)
        {
            throw new IndexOutOfRangeException($"Seek offset out of range: 0x{offset:X8}");
        }

        _position = offset;
    }

    public void WriteByte(int b)
    {
        if (_position >= _buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: 0x{_position:X8}");
        }
        if (_position > _buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: 0x{_position:X8}");
        }

        _buffer[_position++] = (byte)b;
    }

    public void WriteBytes(IEnumerable<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            WriteByte(b);
        }
    }

    public void WriteInt16(int i)
    {
        WriteByte(i >> 8);
        WriteByte(i);
    }

    public void WriteUInt32(uint i)
    {
        WriteSInt32((int)i);
    }

    public void WriteSInt32(int i)
    {
        WriteInt16(i >> 16);
        WriteInt16(i);
    }

    public void WriteCString(string s)
    {
        int p = 0;

        while (NextByte(s, ref p, out byte b))
        {
            WriteByte(b);
        }

        WriteByte(0);
    }

    private bool NextByte(string s, ref int p, out byte b)
    {
        if (p < s.Length)
        {
            if (!ReadEncodedByteAt(s, ref p, out b))
            {
                b = ReadByteAt(s, ref p);
            }

            return true;
        }

        b = 0;
        return false;
    }

    private byte ReadByteAt(string s, ref int p)
    {
        return Encoding.ASCII.GetBytes(s.Substring(p++, 1))[0];
    }

    private bool ReadEncodedByteAt(string s, ref int p, out byte b)
    {
        int length = s.Length;

        if (length - p >= 4)
        {
            if (s[p] == '`' && s[p + 3] == '`')
            {
                if (TryParseHex(s.Substring(p + 1, 2), out int result))
                {
                    b = (byte)result;
                    p += 4;
                    return true;
                }
            }
        }

        b = 0;

        return false;
    }

    private static bool TryParseHex(string hex, out int result)
    {
        return int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }
}