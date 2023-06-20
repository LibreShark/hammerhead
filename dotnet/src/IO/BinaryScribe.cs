using System.Text;
using Google.Protobuf;

namespace LibreShark.Hammerhead.IO;

// ReSharper disable BuiltInTypeReferenceStyle
using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using s64 = Int64;
using u64 = UInt64;
using f64 = Double;

public abstract class BinaryScribe
{
    protected delegate u16 U16Reader();
    protected delegate u16 U32Reader();

    protected readonly u8[] Buffer;

    protected BinaryScribe(byte[] buffer)
    {
        Buffer = buffer;
    }

    #region Seeking

    public u32 Position { get; protected set; }
    public bool EndReached => Position >= Buffer.Length;

    public BinaryScribe Seek(u32 addr)
    {
        Position = addr;
        CheckBounds();
        return this;
    }

    public TReturn MaintainPosition<TReturn>(Func<TReturn> operation)
    {
        u32 oldAddr = Position;
        TReturn value = operation();
        Position = oldAddr;
        return value;
    }

    protected void CheckBounds()
    {
        if (Position == Buffer.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {Buffer.Length} (0x{Buffer.Length:X8})");
        }
        if (Position > Buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {Position} (0x{Position:X8}). Must be between 0 and {Buffer.Length} (0x{Buffer.Length:X8}).");
        }
    }

    #endregion

    #region Find / Contains

    public s32 Find(string needle)
    {
        return Buffer.Find(needle);
    }

    public s32 Find(byte[] needle)
    {
        return Buffer.Find(needle);
    }

    public bool Contains(string needle)
    {
        return Buffer.Contains(needle);
    }

    public bool Contains(byte[] needle)
    {
        return Buffer.Contains(needle);
    }

    #endregion

    #region Padding detection

    public bool IsPadding()
    {
        u32 addr = Position;
        return MaintainPosition(() =>
        {
            u32 chunk1 = Seek(addr).ReadU32();
            u32 chunk2 = Seek(addr + 4).ReadU32();
            return IsPadding(chunk1) &&
                   IsPadding(chunk2);
        });
    }

    protected virtual bool IsPadding(u32 val)
    {
        return val is 0x00000000 or 0xFFFFFFFF;
    }

    #endregion

    #region Bytes

    public u8[] PeekBytesAt(u32 addr, u32 count)
    {
        if (addr == Buffer.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {Buffer.Length} (0x{Buffer.Length:X8})");
        }
        if (addr + count > Buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {addr+count} (0x{addr+count:X8}). Must be between 0 and {Buffer.Length} (0x{Buffer.Length:X8}).");
        }
        return Buffer.Skip((int)addr).Take((int)count).ToArray();
    }

    public u8[] PeekBytes(u32 count)
    {
        return PeekBytesAt(Position, count);
    }

    public u8[] ReadBytes(u32 count)
    {
        byte[] bytes = PeekBytes(count);
        Position += count;
        return bytes;
    }

    #endregion

    #region Integers: 8-bit

    public u8 ReadU8()
    {
        if (Position == Buffer.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {Buffer.Length} (0x{Buffer.Length:X8})");
        }
        if (Position > Buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {Position} (0x{Position:X8}). Must be between 0 and {Buffer.Length} (0x{Buffer.Length:X8}).");
        }

        return Buffer[Position++];
    }

    public s8 ReadS8()
    {
        return (s8)ReadU8();
    }

    public BinaryScribe WriteU8(u8 value)
    {
        Buffer[Position++] = value;
        return this;
    }

    public BinaryScribe WriteS8(s8 value)
    {
        return WriteU8((u8)value);
    }

    #endregion

    #region Integers: 16-bit

    public abstract u16 ReadU16();
    public abstract BinaryScribe WriteU16(u16 value);

    public s16 ReadS16()
    {
        return (s16)ReadU16();
    }

    public BinaryScribe WriteS16(s16 value)
    {
        return WriteU16((u16)value);
    }

    #endregion

    #region Integers: 32-bit

    public abstract u32 ReadU32();
    public abstract BinaryScribe WriteU32(u32 value);

    public s32 ReadS32()
    {
        return (s32)ReadU32();
    }

    public BinaryScribe WriteS32(s32 value)
    {
        return WriteU32((u32)value);
    }

    #endregion

    #region Strings

    private delegate bool ByteToStrTranslator(byte b, out string ch);

    public RomString ReadCStringUntilNull(u32 maxLen = 0)
    {
        return ReadCString(maxLen, (byte b, out string ch) =>
        {
            if (b != 0)
            {
                ch = ByteToStr(b);
                return true;
            }
            ch = "";
            return false;
        });
    }

    public RomString ReadPrintableCString(u32 maxLen = 0)
    {
        return ReadCString(maxLen, (byte b, out string ch) =>
        {
            bool isPrintable = b >= ' ' && b <= '~';
            if (isPrintable || b > 127)
            {
                ch = ByteToStr(b);
                return true;
            }
            ch = "";
            return false;
        });
    }

    private RomString ReadCString(u32 maxLen, ByteToStrTranslator byteToStr)
    {
        var sb = new StringBuilder();

        u32 startPos = Position;
        while (true)
        {
            u32 bytesRead = Position - startPos;
            if (maxLen > 0 && bytesRead >= maxLen)
            {
                bool curIsNull = Buffer[Position] == 0;
                if (bytesRead == maxLen && curIsNull)
                {
                    // Account for null terminator in C-style strings
                    Position++;
                }
                break;
            }

            byte b = Buffer[Position];
            if (b == 0)
            {
                // Account for null terminator in C-style strings
                Position++;
                break;
            }

            if (byteToStr(b, out string s))
            {
                sb.Append(s);
                Position++;
            }
            else
            {
                break;
            }
        }

        u32 endPos = Position;
        u32 len = endPos - startPos;

        return new RomString
        {
            Addr = new RomRange
            {
                StartIndex = startPos,
                EndIndex = endPos,
                Length = len,
                RawBytes = ByteString.CopyFrom(Buffer[(int)startPos..(int)endPos]),
            },
            Value = sb.ToString(),
        };
    }

    private static string ByteToStr(byte b)
    {
        if (b > 127)
        {
            return string.Concat('`', b.ToString("X2"), '`');
        }
        else
        {
            return new byte[] { b }.ToAsciiString();
        }
    }

    #endregion
}
