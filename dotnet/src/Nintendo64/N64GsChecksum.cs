using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Nintendo64;

public class N64GsChecksum
{
    public u8 GetCheckDigit(u8[] romFile, u8[] kcBytes)
    {
        var fileScribe = new BigEndianScribe(romFile);
        fileScribe.Seek(0x30);
        u32 n1 = fileScribe.ReadU32();
        u32 n2 = fileScribe.ReadU32();
        u32 n3 = fileScribe.ReadU32();
        u32 n4 = fileScribe.ReadU32();

        u32 checksum = n1 + n2 + n3 + n4;

        var kcScribe = new BigEndianScribe(kcBytes);
        while (kcScribe.Position <= kcBytes.Length - 4)
        {
            u32 num = kcScribe.ReadU32();
            checksum += num;
        }

        return (u8)checksum;
    }
}
