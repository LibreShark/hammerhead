// bacteriamage.wordpress.com

// Based on GSProN64_Crypt.exe by Hanimar (March 5, 2000)
// https://gameshark.fandom.com/wiki/Nintendo_64#GameShark_firmware
// http://web.archive.org/web/20160324145321/http://doc.kodewerx.org/tools/n64/gs_n64_crypt.zip

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Encrypt or decrypt a GameShark ROM image used with the offical N64Utils program.
/// </summary>
class N64GsRomCrypter
{
    public static byte[] Encrypt(byte[] input)
    {
        return ProcessRom(input, encoder => encoder.Encode());
    }

    public static byte[] Decrypt(byte[] input)
    {
        return ProcessRom(input, decoder => decoder.Decode());
    }

    private static byte[] ProcessRom(byte[] input, Action<N64GsRomCrypter> action)
    {
        N64GsRomCrypter crypter = new N64GsRomCrypter(input);
        action(crypter);
        return crypter._output;
    }

    private readonly byte[] _output;
    private readonly N64GsBinReader _reader;
    private readonly N64GsBinWriter _writer;

    private N64GsRomCrypter(byte[] input)
    {
        _output = input.ToArray();
        _reader = new N64GsBinReader(_output);
        _writer = new N64GsBinWriter(_output);
    }

    private static IReadOnlyList<uint> Seeds { get; } = new uint[]
    {
        0x1471332e, 0x8149432e, 0x75697b21, 0x15597883,
        0x1c2ad435, 0x13ade834, 0xe2de18b1, 0x51bc7835,
        0x158732d4, 0x68d77612, 0x55424441, 0xd1f3fe22,
        0xaeed7894, 0x34685312, 0xa3266563, 0x452cc12e,
    };

    private void Encode()
    {
        ForEachUInt((value, seed) => (value + (seed & 0xff00)) ^ seed);
    }

    private void Decode()
    {
        ForEachUInt((value, seed) => (value ^ seed) - (seed & 0xff00));
    }

    private void ForEachUInt(Func<uint, uint, uint> formula)
    {
        _reader.Seek(0);

        while (!_reader.EndReached)
        {
            int addr = (int)((_reader.Position >> 2) & 0x0F);
            uint seed = Seeds[addr];
            uint value = GetNextUInt();

            PutNextUInt(formula(value, seed));
        }
    }

    private uint GetNextUInt()
    {
        return GetNextByte() + (GetNextByte() << 8) + (GetNextByte() << 16) + (GetNextByte() << 24);
    }

    private void PutNextUInt(uint value)
    {
        PutNextByte(value);
        PutNextByte(value >> 8);
        PutNextByte(value >> 16);
        PutNextByte(value >> 24);
    }

    private uint GetNextByte()
    {
        return _reader.ReadUByte();
    }

    private void PutNextByte(uint value)
    {
        _writer.WriteByte(value);
    }
}
