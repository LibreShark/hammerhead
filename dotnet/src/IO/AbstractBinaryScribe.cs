using System.Text;
using Google.Protobuf;

namespace LibreShark.Hammerhead.IO;

public abstract class AbstractBinaryScribe
{
    protected u8[] BufferRef;

    protected AbstractBinaryScribe(u8[] bufferRef)
    {
        BufferRef = bufferRef;
    }

    public u8[] GetBufferCopy()
    {
        return BufferRef.ToArray();
    }

    public void ResetBuffer(u8[] bufferRef)
    {
        BufferRef = bufferRef;
        Position = 0;
    }

    #region Seeking

    public u32 Position { get; protected set; }
    public bool EndReached => Position >= BufferRef.Length;

    public AbstractBinaryScribe Seek(u32 addr)
    {
        Position = addr;
        CheckBounds();
        return this;
    }

    public AbstractBinaryScribe Seek(s32 addr)
    {
        return Seek((u32)addr);
    }

    public AbstractBinaryScribe Skip(u32 count)
    {
        Position += count;
        CheckBounds();
        return this;
    }

    public AbstractBinaryScribe Skip(s32 count)
    {
        return Skip((u32)count);
    }

    public AbstractBinaryScribe Next()
    {
        return Skip(1);
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
        if (Position == BufferRef.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {BufferRef.Length} (0x{BufferRef.Length:X8})");
        }
        if (Position > BufferRef.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {Position} (0x{Position:X8}). Must be between 0 and {BufferRef.Length} (0x{BufferRef.Length:X8}).");
        }
    }

    #endregion

    #region Find / Contains

    public s32 Find(string needle)
    {
        return BufferRef.Find(needle);
    }

    public s32 Find(u8[] needle)
    {
        return BufferRef.Find(needle);
    }

    public bool Contains(string needle)
    {
        return BufferRef.Contains(needle);
    }

    public bool Contains(u8[] needle)
    {
        return BufferRef.Contains(needle);
    }

    #endregion

    #region Data type detection

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

    public bool IsPrintableChar()
    {
        char c = (char)BufferRef[Position];
        return c is >= ' ' and <= '~';
    }

    public bool IsIntegerDigit()
    {
        char c = (char)BufferRef[Position];
        return c is >= '0' and <= '9';
    }

    public bool IsFloatDigit()
    {
        char c = (char)BufferRef[Position];
        return c is >= '0' and <= '9' or '.';
    }

    #endregion

    #region Bytes

    public u8[] PeekBytesAt(u32 addr, u32 count)
    {
        if (addr == BufferRef.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {BufferRef.Length} (0x{BufferRef.Length:X8})");
        }
        if (addr + count > BufferRef.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {addr+count} (0x{addr+count:X8}). Must be between 0 and {BufferRef.Length} (0x{BufferRef.Length:X8}).");
        }
        return BufferRef.Skip((int)addr).Take((int)count).ToArray();
    }

    public u8[] PeekBytes(u32 count)
    {
        return PeekBytesAt(Position, count);
    }

    public u8[] ReadBytes(u32 count)
    {
        u8[] bytes = PeekBytes(count);
        Position += count;
        return bytes;
    }

    #endregion

    #region Integers: 8-bit

    public u8 ReadU8()
    {
        if (Position == BufferRef.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {BufferRef.Length} (0x{BufferRef.Length:X8})");
        }
        if (Position > BufferRef.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {Position} (0x{Position:X8}). Must be between 0 and {BufferRef.Length} (0x{BufferRef.Length:X8}).");
        }

        return BufferRef[Position++];
    }

    public s8 ReadS8()
    {
        return (s8)ReadU8();
    }

    public AbstractBinaryScribe WriteU8(u8 value)
    {
        BufferRef[Position++] = value;
        return this;
    }

    public AbstractBinaryScribe WriteS8(s8 value)
    {
        return WriteU8((u8)value);
    }

    public bool ReadBool()
    {
        u8 b = ReadU8();
        return b != 0;
    }

    public AbstractBinaryScribe WriteBool(bool value)
    {
        WriteU8((u8)(value ? 1 : 0));
        return this;
    }

    public TEnum ReadEnum8<TEnum>() where TEnum : Enum
    {
        u8 b = ReadU8();
        return (TEnum)Enum.ToObject(typeof(TEnum), b);
    }

    #endregion

    #region Integers: 16-bit

    public abstract u16 ReadU16();
    public abstract AbstractBinaryScribe WriteU16(u16 value);

    public s16 ReadS16()
    {
        return (s16)ReadU16();
    }

    public AbstractBinaryScribe WriteS16(s16 value)
    {
        return WriteU16((u16)value);
    }

    #endregion

    #region Integers: 32-bit

    public abstract u32 ReadU32();
    public abstract AbstractBinaryScribe WriteU32(u32 value);

    public s32 ReadS32()
    {
        return (s32)ReadU32();
    }

    public AbstractBinaryScribe WriteS32(s32 value)
    {
        return WriteU32((u32)value);
    }

    #endregion

    #region Strings

    private delegate bool ByteToStrTranslator(byte b, out string ch);

    public RomString ReadCStringUntil(u32 maxLen = 0, char terminator = '\n')
    {
        return ReadCString(maxLen, (byte b, out string ch) =>
        {
            bool equalsTerminator = b == 0 || b == terminator;
            bool isNewlineTerminator =
                (terminator is '\n' or '\r' or '\f') &&
                ((char)b is '\n' or '\r' or '\f');
            if (equalsTerminator || isNewlineTerminator)
            {
                ch = "";
                return false;
            }

            ch = ByteToStr(b);
            return true;
        }, false);
    }

    public RomString ReadCStringUntilNull(u32 maxLen = 0, bool isNullTerminated = true)
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
        }, isNullTerminated);
    }

    public RomString ReadFixedLengthPrintableCString(u32 len)
    {
        u32 startPos = Position;
        RomString romStr = ReadPrintableCString(len, false).Trim();
        u32 endPos = startPos + len;
        if (endPos < BufferRef.Length)
        {
            // In case the string contains an unexpected null byte and stops
            // reading early, make sure we seek to the right end position.
            Seek(endPos);
        }
        return romStr;
    }

    public RomString ReadPrintableCString(u32 maxLen = 0, bool isNullTerminated = true)
    {
        return ReadCString(maxLen, (byte b, out string ch) =>
        {
            bool isPrintable = b >= ' ' && b <= '~';
            // TODO(CheatoBaggins): Remove `b > 127` check; it's only for N64 GameShark
            if (isPrintable || b > 127)
            {
                ch = ByteToStr(b);
                return true;
            }
            ch = "";
            return false;
        }, isNullTerminated);
    }

    private RomString ReadCString(u32 maxLen, ByteToStrTranslator byteToStr, bool isNullTerminated)
    {
        var sb = new StringBuilder();

        u32 startPos = Position;
        while (!EndReached)
        {
            byte b = BufferRef[Position];
            bool isNull = b == 0;
            u32 bytesRead = Position - startPos;

            if (maxLen > 0 && bytesRead >= maxLen)
            {
                if (isNullTerminated && bytesRead == maxLen && isNull)
                {
                    // Account for null terminator in C-style strings
                    Position++;
                }
                break;
            }

            if (isNull)
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
                RawBytes = ByteString.CopyFrom(BufferRef[(int)startPos..(int)endPos]),
            },
            Value = sb.ToString(),
        };
    }

    private static string ByteToStr(u8 b)
    {
        if (b == '\0') return @"\0";
        if (b == '\t') return @"\t";
        if (b == '\n') return @"\n";
        if (b == '\r') return @"\r";
        if (b == '\f') return @"\f";
        if (b < ' ')
        {
            return $"[0x{b:X2}]";
        }
        return new u8[] { b }.ToAsciiString();
    }

    #endregion

    public void WriteBytes(ByteString byteString)
    {
        WriteBytes(byteString.ToByteArray());
    }

    public AbstractBinaryScribe WriteBytes(u8[] bytes)
    {
        Array.Copy(bytes, 0, BufferRef, (s32)Position, bytes.Length);
        Position += (u32)bytes.Length;
        return this;
    }

    public void WriteCString(RomString str, int maxLen = 0, bool isNullTerminated = true)
    {
        WriteCString(str.Value, maxLen, isNullTerminated);
    }

    public AbstractBinaryScribe WriteCString(string str, int maxLen = 0, bool isNullTerminated = true)
    {
        if (maxLen > 0 && str.Length > maxLen)
        {
            str = str[..maxLen];
        }
        var bytes = str.ToAsciiBytes();
        if (isNullTerminated && bytes.Last() != 0)
        {
            // C strings must be null-terminated
            bytes = bytes.Concat(new u8[] { 0 }).ToArray();
        }
        WriteBytes(bytes);
        return this;
    }

    public override string ToString()
    {
        if (EndReached)
        {
            return $"AT 0x{Position:X8}: N/A (length = 0x{BufferRef.Length:X8})";
        }

        u8 b = BufferRef[Position];
        char c = (char)b;
        return $"AT 0x{Position:X8}: 0x{b:X2} = '{c}' (length = 0x{BufferRef.Length:X8})";
    }
}
