// bacteriamage.wordpress.com

// Based on GSProN64_Crypt.exe by Hanimar (March 5, 2000)
// https://gameshark.fandom.com/wiki/Nintendo_64#GameShark_firmware
// http://web.archive.org/web/20160324145321/http://doc.kodewerx.org/tools/n64/gs_n64_crypt.zip

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Encrypt or decrypt a GameShark ROM image used with the offical N64Utils program.
/// </summary>
class RomCrypter : RomBase
{
    #region File API

    public static void EncodeFile(string path)
    {
		EncodeFile(path, path);
    }

	public static void EncodeFile(string source, string target)
	{
        ProcessRom(source, target, encoder => encoder.Encode());
	}

	public static void DecodeFile(string path)
	{
		DecodeFile(path, path);
	}

	public static void DecodeFile(string source, string target)
	{
		ProcessRom(source, target, decoder => decoder.Decode());
	}

	private static void ProcessRom(string source, string target, Action<RomCrypter> action)
	{
		RomCrypter crypter = new RomCrypter(N64BinaryReader.FromFile(source));
		action(crypter);
		crypter.WriteRomToFile(target);
	}

	private RomCrypter(N64BinaryReader reader)
		: base(reader)
	{
	}

    #endregion

    #region Encryption

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
		SeekStart();

		while (!Reader.EndReached)
		{
			uint seed = Seeds[(Reader.Position >> 2) & 0x0f];
			uint value = GetNextUInt();

			PutNextUInt(formula(value, seed));
		}
	}

    #endregion

    #region Buffer access

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
		return (uint)Reader.ReadUByte();
	}

	private void PutNextByte(uint value)
	{
		Writer.WriteByte((int)value);
	}

    #endregion
}
