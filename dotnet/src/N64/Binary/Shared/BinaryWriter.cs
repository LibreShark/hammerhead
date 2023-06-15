// bacteriamage.wordpress.com

using System.Globalization;
using System.Text;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Helper class for writing (big-endian) integers and c-style strings to byte buffers.
/// </summary>
class BinaryWriter
{
    public byte[]? Buffer { get; set; }
    public int Position { get; set; }
    public int BytesWritten { get; set; }
    public int AutoExtendSize { get; set; }

    public BinaryWriter(int bufferSize)
        : this(new byte[bufferSize])
    {
    }

    public BinaryWriter(byte[] buffer)
        : this()
    {
        Buffer = buffer;
    }

    public BinaryWriter()
    {
    }

    public void WriteToFile(string path)
    {
        File.WriteAllBytes(path, Buffer ?? Array.Empty<byte>());
    }

    public void Seek(int position)
    {
        Position = position;
    }

    public void SeekEnd()
    {
        Seek(Buffer?.Length ?? 0);
    }

    public void SeekOffset(int offset)
    {
        offset += Position;

        if (offset < 0 || Buffer == null || offset >= Buffer.Length)
        {
            throw new IndexOutOfRangeException("Seek offset out of range");
        }

        Position = offset;
    }

    public void WriteByte(int b)
    {
        if (Buffer == null || Position == Buffer.Length)
        {
            AutoExtend();
        }
        if (Position < 0 || Position > Buffer?.Length)
        {
            throw new IndexOutOfRangeException("Invalid position");
        }

        Buffer![Position++] = (byte)b;

        BytesWritten++;
    }

    public void WriteBytes(IEnumerable<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            WriteByte(b);
        }
    }

    private void AutoExtend()
    {
        if (AutoExtendSize < 1)
        {
            throw new IndexOutOfRangeException("End of buffer reached");
        }

        byte[] newBuffer;

        if (Buffer == null)
        {
            newBuffer = new byte[AutoExtendSize];
        }
        else
        {
            newBuffer = new byte[Buffer.Length + AutoExtendSize];
            Array.Copy(Buffer, newBuffer, Buffer.Length);
        }

        Buffer = newBuffer;
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

    private bool TryParseHex(string hex, out int result)
    {
        return int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }
}
