// bacteriamage.wordpress.com

using System.Text;

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

/// <summary>
/// Helper class for reading (big-endian) integers and c-style strings from byte buffers.
///
/// TODO(CheatoBaggins): Add transformer lambdas for replacing GS chars (e.g., "Infinite ")
/// </summary>
internal class BigEndianReader : IBinReader
{
    private readonly u8[] _buffer;

    public u32 Position { get; private set; }
    public bool EndReached => Position >= _buffer.Length;

    public BigEndianReader(byte[] buffer)
    {
        _buffer = buffer;
    }

    #region Seeking

    public IBinReader Seek(u32 addr)
    {
        Position = addr;
        CheckBounds();
        return this;
    }

    private TReturn MaintainPosition<TReturn>(Func<TReturn> operation)
    {
        u32 oldAddr = Position;
        TReturn value = operation();
        Position = oldAddr;
        return value;
    }

    private void CheckBounds()
    {
        if (Position == _buffer.Length)
        {
            throw new IndexOutOfRangeException($"End of buffer reached: {_buffer.Length} (0x{_buffer.Length:X8})");
        }
        if (Position > _buffer.Length)
        {
            throw new IndexOutOfRangeException($"Invalid position: {Position} (0x{Position:X8}). Must be between 0 and {_buffer.Length} (0x{_buffer.Length:X8}).");
        }
    }

    #endregion

    #region Find / Contains

    public s64 Find(string needle)
    {
        return _buffer.Find(needle);
    }

    public s64 Find(byte[] needle)
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

    #endregion

    #region Padding detection

    public bool IsSectionPadding()
    {
        return IsSectionPaddingAt(Position);
    }

    public bool IsSectionPaddingAt(u32 addr)
    {
        return MaintainPosition(() =>
        {
            u32 chunk1 = Seek(addr).ReadU32();
            u32 chunk2 = Seek(addr + 4).ReadU32();
            return IsPadding(chunk1) &&
                   IsPadding(chunk2);
        });
    }

    private static bool IsPadding(u32 val)
    {
        return val is 0x00000000 or 0xFFFFFFFF;
    }

    #endregion

    #region Bytes

    public u8[] PeekBytesAt(u32 addr, u32 count)
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

    #region Integers

    public u8 ReadU8()
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

    public s8 ReadS8()
    {
        return (s8)ReadU8();
    }

    public u16 ReadU16()
    {
        byte b1 = ReadU8();
        byte b2 = ReadU8();
        s32 value = (b1 << 8) + b2;
        return (u16) value;
    }

    public s16 ReadS16()
    {
        return (s16)ReadU16();
    }

    public u32 ReadU32()
    {
        u32 high = ReadU16();
        u32 low = ReadU16();

        return (high << 16) + low;
    }

    public s32 ReadS32()
    {
        return (s32)ReadU32();
    }

    #endregion

    #region Strings

    public RomString ReadCString(u32 maxLen = 0)
    {
        return ReadCString((out string ch) => NextCharacter(out ch), maxLen);
    }

    public RomString ReadCStringAt(u32 addr, u32 maxLen = 0)
    {
        Seek(addr);
        return ReadCString((out string ch) => NextCharacter(out ch), maxLen);
    }

    public RomString ReadPrintableCString(u32 maxLen = 0)
    {
        return ReadCString((out string ch) => NextPrintableCharacter(out ch), maxLen);
    }

    public RomString ReadPrintableCStringAt(u32 addr, u32 maxLen = 0)
    {
        Seek(addr);
        return ReadCString((out string ch) => NextPrintableCharacter(out ch), maxLen);
    }

    private RomString ReadCString(TryReadNextChar read, u32 maxLen)
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
            return new byte[] { b }.ToAsciiString();
        }
    }

    private bool NextRawChar(out byte b)
    {
        b = ReadU8();
        return b != 0;
    }

    #endregion
}
