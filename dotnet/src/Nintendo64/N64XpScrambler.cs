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
/// Scrambles/unscrambles Xplorer 64 ROM files dumped directly from the two
/// EEPROM chips, and re-scrambles them for flashing the EEPROMs directly.
/// </summary>
public static class N64XpScrambler
{
    public static u8[] ScrambleRom(u8[] plainBytes)
    {
        u8[] high = new u8[plainBytes.Length / 2];
        u8[] low = new u8[plainBytes.Length / 2];

        for (int i = 0; i < plainBytes.Length; i += 2)
        {
            high[i / 2] = plainBytes[i];
            low[i / 2] = plainBytes[i + 1];
        }

        u8[] unscrambledHigh = new u8[plainBytes.Length / 2];
        u8[] unscrambledLow = new u8[plainBytes.Length / 2];

        for (int i = 0; i < (plainBytes.Length / 2); i++)
        {
            int scrambledAddress = UnscrambleAddr(i);
            unscrambledHigh[i] = high[scrambledAddress];
            unscrambledLow[i] = low[scrambledAddress];
        }

        u8[] scrambledBytes = unscrambledHigh
            .Zip(unscrambledLow, (a, b) => new[] { a, b })
            .SelectMany(ab => ab)
            .ToArray();
        return scrambledBytes;
    }

    public static u8[] UnscrambleRom(u8[] scrambledBytes)
    {
        u8[] high = new u8[scrambledBytes.Length / 2];
        u8[] low = new u8[scrambledBytes.Length / 2];

        for (int i = 0; i < scrambledBytes.Length; i += 2)
        {
            high[i / 2] = scrambledBytes[i];
            low[i / 2] = scrambledBytes[i + 1];
        }

        u8[] scrambledHigh = new u8[scrambledBytes.Length / 2];
        u8[] scrambledLow = new u8[scrambledBytes.Length / 2];

        for (int i = 0; i < (scrambledBytes.Length / 2); i++)
        {
            int unscrambledAddress = ScrambleAddr(i);
            scrambledHigh[i] = high[unscrambledAddress];
            scrambledLow[i] = low[unscrambledAddress];
        }

        u8[] plainBytes = scrambledHigh
            .Zip(scrambledLow, (a, b) => new[] { a, b })
            .SelectMany(ab => ab)
            .ToArray();
        return plainBytes;
    }

    private static int UnscrambleAddr(int addr)
    {
        return (((addr >> 4) & 0x001) | ((addr >> 8) & 0x002) | ((~addr >> 9) & 0x004) | ((addr >> 3) & 0x008) |
                ((addr >> 6) & 0x010) | ((addr >> 2) & 0x020) | ((~addr << 5) & 0x0C0) | ((~addr << 8) & 0x100) |
                ((~addr << 6) & 0x200) | ((~addr << 2) & 0x400) | ((addr << 6) & 0x800) | (addr & 0x1F000));
    }

    private static int ScrambleAddr(int addr)
    {
        return (((~addr >> 8) & 0x001) | ((~addr >> 5) & 0x006) | ((~addr >> 6) & 0x008) | ((addr << 4) & 0x010) |
                ((addr >> 6) & 0x020) | ((addr << 3) & 0x040) | ((addr << 2) & 0x080) | ((~addr >> 2) & 0x100) |
                ((addr << 8) & 0x200) | ((addr << 6) & 0x400) | ((~addr << 9) & 0x800) | (addr & 0x1F000));
    }
}
