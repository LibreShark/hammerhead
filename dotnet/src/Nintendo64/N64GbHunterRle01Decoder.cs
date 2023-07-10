using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Nintendo64;

/// <summary>
/// EXPERIMENTAL (UNTESTED) RLE01 decoder for N64 GB Hunter and Game Booster ROM asset compression.
/// Adapted from https://segaxtreme.net/threads/action-replay-save-format.24708/post-180339
/// </summary>
public class N64GbHunterRle01Decoder
{
    // 0x002897dc ARP_202C.BIN
    public static s32 DecompressRle01(byte[] src, byte[] dest)
    {
        /*
        i = 0;
        do {
            local_14[i] = src[i];
            i = i + 1;
        } while (i < 5);
        */
        u8 rleKey = src[5];

        u32 compressedSize = new BigEndianScribe(src[6..10]).ReadU32();

        Console.WriteLine($"Compressed size is 0x{compressedSize:X8} = {compressedSize} bytes");

        s32 j = 0;
        u32 i = 10;
        do {
            if (src[i] == rleKey) {
                u32 count = (u32)(char)src[i + 1] & 0xff;
                if (count == 0) {
                    dest[j] = rleKey;
                    i += 2;
                    j += 1;
                    if (compressedSize <= i) {
                        return j;
                    }
                    continue;
                }
                u8 val = src[i + 2];
                i += 3;
                u32 k = 0;
                do {
                    dest[j] = val;
                    j += 1;
                    k += 1;
                } while (k < count);
            }
            else {
                dest[j] = src[i];
                i += 1;
                j += 1;
            }
            if (compressedSize <= i) {
                return j;
            }
        } while(true);
    }
}
