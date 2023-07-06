// bacteriamage.wordpress.com

// Based on GSProN64_Crypt.exe by Hanimar (March 5, 2000)
// https://gameshark.fandom.com/wiki/Nintendo_64#GameShark_firmware
// http://web.archive.org/web/20160324145321/http://doc.kodewerx.org/tools/n64/gs_n64_crypt.zip

using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Nintendo64;

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
/// Encrypt or decrypt a GameShark ROM image for use with Datel's official
/// N64 Utils PC program, which requires encrypted ROMs.
/// </summary>
class N64GsCrypter
{
    public static byte[] Encrypt(IReadOnlyList<byte> input)
    {
        return ProcessRom(input, encoder => encoder.Encode());
    }

    public static byte[] Decrypt(IReadOnlyList<byte> input)
    {
        return ProcessRom(input, decoder => decoder.Decode());
    }

    private static byte[] ProcessRom(IReadOnlyList<byte> input, Action<N64GsCrypter> action)
    {
        var crypter = new N64GsCrypter(input);
        action(crypter);
        return crypter._writer.GetBufferCopy();
    }

    private readonly BigEndianScribe _reader;
    private readonly BigEndianScribe _writer;

    private N64GsCrypter(IReadOnlyList<byte> input)
    {
        _reader = new BigEndianScribe(input.ToArray());
        _writer = new BigEndianScribe(input.ToArray());
    }

    private static IReadOnlyList<u32> Seeds { get; } = new u32[]
    {
        0x1471332e, 0x8149432e, 0x75697b21, 0x15597883,
        0x1c2ad435, 0x13ade834, 0xe2de18b1, 0x51bc7835,
        0x158732d4, 0x68d77612, 0x55424441, 0xd1f3fe22,
        0xaeed7894, 0x34685312, 0xa3266563, 0x452cc12e,
    };

    private void Encode()
    {
        ForEachU32((value, seed) => (value + (seed & 0xff00)) ^ seed);
    }

    private void Decode()
    {
        ForEachU32((value, seed) => (value ^ seed) - (seed & 0xff00));
    }

    private void ForEachU32(Func<u32, u32, u32> formula)
    {
        _reader.Seek(0);

        while (!_reader.EndReached)
        {
            s32 addr = (s32)((_reader.Position >> 2) & 0x0F);
            u32 seed = Seeds[addr];
            u32 value = GetNextU32();

            PutNextU32(formula(value, seed));
        }
    }

    private uint GetNextU32()
    {
        return GetNextByte() + (GetNextByte() << 8) + (GetNextByte() << 16) + (GetNextByte() << 24);
    }

    private void PutNextU32(u32 value)
    {
        PutNextByte(value);
        PutNextByte(value >> 8);
        PutNextByte(value >> 16);
        PutNextByte(value >> 24);
    }

    private uint GetNextByte()
    {
        return _reader.ReadU8();
    }

    /// <param name="singleByteValue">
    /// A u8 value, typed as a u32 to make the callsite more readable.
    /// </param>
    private void PutNextByte(u32 singleByteValue)
    {
        _writer.WriteU8((u8)(singleByteValue & 0xFF));
    }
}
