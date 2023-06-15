namespace LibreShark.Hammerhead.N64;

public class N64XplorerScrambler
{
    public static int Unscramble(int addr)
    {
        return (((addr >> 4) & 0x001) | ((addr >> 8) & 0x002) | ((~addr >> 9) & 0x004) | ((addr >> 3) & 0x008) |
                ((addr >> 6) & 0x010) | ((addr >> 2) & 0x020) | ((~addr << 5) & 0x0C0) | ((~addr << 8) & 0x100) |
                ((~addr << 6) & 0x200) | ((~addr << 2) & 0x400) | ((addr << 6) & 0x800) | (addr & 0x1F000));
    }

    public static int Scramble(int addr)
    {
        return (((~addr >> 8) & 0x001) | ((~addr >> 5) & 0x006) | ((~addr >> 6) & 0x008) | ((addr << 4) & 0x010) |
                ((addr >> 6) & 0x020) | ((addr << 3) & 0x040) | ((addr << 2) & 0x080) | ((~addr >> 2) & 0x100) |
                ((addr << 8) & 0x200) | ((addr << 6) & 0x400) | ((~addr << 9) & 0x800) | (addr & 0x1F000));
    }

    public static byte[] ScrambleXpRom(byte[] plainBytes)
    {
        byte[] scrambledBytes = new byte[plainBytes.Length];
        byte[] high = new byte[plainBytes.Length / 2];
        byte[] low = new byte[plainBytes.Length / 2];

        for (int i = 0; i < (plainBytes.Length / 2); i += 2)
        {
            high[i / 2] = plainBytes[i];
            low[i / 2] = plainBytes[i + 1];
        }

        byte[] unscrambledHigh = new byte[plainBytes.Length / 2];
        byte[] unscrambledLow = new byte[plainBytes.Length / 2];

        for (int i = 0; i < (plainBytes.Length / 2); i++)
        {
            int scrambledAddress = Unscramble(i);
            unscrambledHigh[i] = high[scrambledAddress];
            unscrambledLow[i] = low[scrambledAddress];
        }

        scrambledBytes = unscrambledHigh.Zip(unscrambledLow, (a, b) => new[] { a, b }).SelectMany(ab => ab).ToArray();
        return scrambledBytes;
    }

    public static byte[] UnscrambleXpRom(byte[] scrambledBytes)
    {
        byte[] plainBytes = new byte[scrambledBytes.Length];
        byte[] high = new byte[scrambledBytes.Length / 2];
        byte[] low = new byte[scrambledBytes.Length / 2];

        for (int i = 0; i < scrambledBytes.Length; i += 2)
        {
            high[i / 2] = scrambledBytes[i];
            low[i / 2] = scrambledBytes[i + 1];
        }

        byte[] scrambledHigh = new byte[scrambledBytes.Length / 2];
        byte[] scrambledLow = new byte[scrambledBytes.Length / 2];

        for (int i = 0; i < (scrambledBytes.Length / 2); i++)
        {
            int unscrambledAddress = Unscramble(i);
            scrambledHigh[i] = high[unscrambledAddress];
            scrambledLow[i] = low[unscrambledAddress];
        }

        plainBytes = scrambledHigh.Zip(scrambledLow, (a, b) => new[] { a, b }).SelectMany(ab => ab).ToArray();

        return plainBytes;
    }
}
