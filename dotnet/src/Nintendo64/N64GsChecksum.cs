using LibreShark.Hammerhead.IO;
using NeoSmart.PrettySize;

namespace LibreShark.Hammerhead.Nintendo64;

/// <summary>
/// <para>
/// Computes key code values and check digits for N64 GS ROM files.
/// </para>
/// <para>
/// This is a C# port of github.com/RWeick's fabulous Python implementation.
/// </para>
/// </summary>
public class N64GsChecksum
{
    private const s32 PROGRAM_SIZE = 0x100000;
    private const s32 FIRMWARE_CHECKSUM_RANGE = 0x1f000;

    private static readonly u32[] IPL3_6105_TABLE = new u32[]
    {
        0xad170014, 0x3c09a600, 0x10000003, 0x25290000,
        0x3c09b000, 0x25290000, 0xad090008, 0x8d0900f0,
        0x3c0bb000, 0xad090018, 0x8d680010, 0x14e80006,
        0x00000000, 0x8d680014, 0x16080003, 0x00000000,
        0x04110003, 0x00000000, 0x0411ffff, 0x00000000,
        0x3c08a400, 0x25080000, 0x8fb00014, 0x8fbf001c,
        0x27bd0020, 0x21092000, 0x25080004, 0x1509fffe,
        0xad09fffc, 0x3c0bb000, 0x8d690008, 0x01200008,
        0x00000000, 0x40083800, 0x400b0800, 0xc80c2000,
        0x8c040040, 0x00000000, 0x00000000, 0x40800000,
        0x38030180, 0x40830800, 0x40801000, 0x3c050020,
        0x04a0001b, 0x40033800, 0x1460fffd, 0x20a5ffff,
        0x8c060000, 0x40800000, 0x38030400, 0x40830800,
        0x38030fff, 0x40831000, 0x40033000, 0x1460fffe,
        0x38030ff0, 0x4a0d6b51, 0xc86e2000, 0x2063fff0,
        0x0461fffd, 0x4a0e6b54, 0x3803b120, 0x40830000,
    };

    public static u8 GetCheckDigit(u8[] romFile, u8[] keyCodeBytes)
    {
        var fileScribe = new BigEndianScribe(romFile);
        fileScribe.Seek(0x30);
        u32 n1 = fileScribe.ReadU32();
        u32 n2 = fileScribe.ReadU32();
        u32 n3 = fileScribe.ReadU32();
        u32 n4 = fileScribe.ReadU32();

        u32 checksum = n1 + n2 + n3 + n4;

        var kcScribe = new BigEndianScribe(keyCodeBytes);
        while (kcScribe.Position <= keyCodeBytes.Length - 4)
        {
            u32 num = kcScribe.ReadU32();
            checksum += num;
        }

        return (u8)checksum;
    }

    private static (u32, u32) ComputeChecksums(u8[] data, N64KeyCodeId cic)
    {
        if (data.Length < PROGRAM_SIZE)
        {
            throw new ArgumentException(
                $"Data size should be >= 1 MiB, but file is only " +
                $"{data.Length} (0x{data.Length:X} or {PrettySize.Format(data.Length)})");
        }

        var scribe = new BigEndianScribe(data);
        var words = new List<u32>();
        while (!scribe.EndReached)
        {
            words.Add(scribe.ReadU32());
        }

        u32 checksum;
        switch (cic)
        {
            case N64KeyCodeId.Diddy:
                checksum = 0xa3886759;
                break;
            case N64KeyCodeId.Zelda:
                checksum = 0xdf26f436;
                break;
            case N64KeyCodeId.Yoshi:
                checksum = 0x1fea617a;
                break;
            default:
                checksum = 0xf8ca4ddc;
                break;
        }

        u32 acc1 = checksum;
        u32 acc2 = checksum;
        u32 acc3 = checksum;
        u32 acc4 = checksum;
        u32 acc5 = checksum;
        u32 acc6 = checksum;

        var ipl36105Table = new List<u32>();

        foreach (u32 current in words)
        {
            if (ipl36105Table.Count == 0)
            {
                ipl36105Table.AddRange(IPL3_6105_TABLE);
            }

            u32 rotated = RotateLeft(current, (s32)(current & 0x1f));

            acc1 = (acc1 + current) & 0xffffffff;

            if (acc1 < current)
                acc2 = (acc2 + 1) & 0xffffffff;

            acc3 ^= current;

            acc4 = (acc4 + rotated) & 0xffffffff;

            if (acc5 > current)
                acc5 ^= rotated;
            else
                acc5 ^= current ^ acc1;

            if (cic == N64KeyCodeId.Zelda)
            {
                u32 currentIpl3 = ipl36105Table.First();
                ipl36105Table.RemoveAt(0);
                acc6 = (acc6 + (current ^ currentIpl3)) & 0xffffffff;
            }
            else
            {
                acc6 = (acc6 + (current ^ acc4)) & 0xffffffff;
            }
        }

        switch (cic)
        {
            case N64KeyCodeId.Diddy:
                return ((acc1 ^ acc2) + acc3, (acc4 ^ acc5) + acc6);
            case N64KeyCodeId.Yoshi:
                return (acc1 * acc2 + acc3, acc4 * acc5 + acc6);
            default:
                return (acc1 ^ acc2 ^ acc3, acc4 ^ acc5 ^ acc6);
        }
    }

    private static u32 RotateLeft(u32 value, int shift)
    {
        return ((value << shift) & 0xffffffff) | (value >> (32 - shift));
    }

    public static byte[] ComputeKeyCode(u8[] romFile, N64KeyCodeId cic)
    {
        using MemoryStream firmware = new MemoryStream(romFile);
        const int IPL3_SIZE = 0x1000;

        firmware.Seek(IPL3_SIZE, SeekOrigin.Begin);
        u8[] data = new u8[FIRMWARE_CHECKSUM_RANGE];
        int bytesRead = firmware.Read(data, 0, FIRMWARE_CHECKSUM_RANGE);

        if (bytesRead < PROGRAM_SIZE)
        {
            int paddingSize = PROGRAM_SIZE - FIRMWARE_CHECKSUM_RANGE;
            Array.Resize(ref data, PROGRAM_SIZE);
            Array.Clear(data, bytesRead, paddingSize);
        }

        (u32 chk1, u32 chk2) = ComputeChecksums(data, cic);

        var scribe = new BigEndianScribe(new byte[13]);
        scribe.WriteU32(chk1).WriteU32(chk2);
        u32 pcEntryAddr = cic switch
        {
            N64KeyCodeId.Diddy => 0x80201000,
            N64KeyCodeId.Zelda => 0x80190000,
            N64KeyCodeId.Yoshi => 0x80200400,
            _                  => 0x80180000,
        };
        scribe.WriteU32(pcEntryAddr);
        u8 checkDigit = GetCheckDigit(romFile, scribe.GetBufferCopy());
        scribe.WriteBytes(new u8[] { checkDigit });
        return scribe.GetBufferCopy();
    }
}
