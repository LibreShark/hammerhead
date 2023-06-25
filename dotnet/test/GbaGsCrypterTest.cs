using LibreShark.Hammerhead.Gba;

namespace LibreShark.Hammerhead.Test.Gba;

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

[TestFixture]
public class GbaGsCrypterTest
{
    private static readonly u32[][] EncryptedCodes = {
        new u32[] { 0xD925CBB3, 0x457D65D3 },
        new u32[] { 0xB6FA891B, 0xDC4362A9 },
        new u32[] { 0x5C057862, 0x28B943D9 },
        new u32[] { 0xACF82924, 0x05A5BBE6 },
        new u32[] { 0x6E173550, 0x7785EF49 },
        new u32[] { 0xE01CBE71, 0x6A981455 },
        new u32[] { 0x26E57C50, 0x09E57C3B },
        new u32[] { 0x57EED658, 0x3820A9E8 },
        new u32[] { 0x5ECBD26F, 0xCB3A0DE2 },
        new u32[] { 0x7579F559, 0x2763862F },
        new u32[] { 0x65EB699D, 0xCA505B93 },
        new u32[] { 0x6E776811, 0x33AAEC31 },
        new u32[] { 0x3F01E5D8, 0xAF93AE9D },
        new u32[] { 0xC233B769, 0x2A883C9B },
    };

    private static readonly u32[][] DecryptedCodes = {
        new u32[] { 0xC4000970, 0x00008401 },
        new u32[] { 0x45584D42, 0x001DC0DE },
        new u32[] { 0x00000000, 0x183EC7FD },
        new u32[] { 0x00000668, 0x00000000 },
        new u32[] { 0x043061EC, 0x2067B507 },
        new u32[] { 0x043061F0, 0x80084906 },
        new u32[] { 0x043061F4, 0x30094678 },
        new u32[] { 0x043061F8, 0x46C04686 },
        new u32[] { 0x043061FC, 0x47004804 },
        new u32[] { 0x04306200, 0x80082007 },
        new u32[] { 0x04306204, 0xF7FBBC07 },
        new u32[] { 0x04306208, 0xBD00FFF1 },
        new u32[] { 0x0430620C, 0x0800009C },
        new u32[] { 0x04306210, 0x09FE0401 },
    };

    [Test]
    public void TestDecrypt()
    {
        var crypter = new GbaGsCrypter(true);

        for (int i = 0; i < EncryptedCodes.Length; i++)
        {
            u32 addrEnc = EncryptedCodes[i][0];
            u32 valueEnc = EncryptedCodes[i][1];

            u32 addrDec = addrEnc;
            u32 valueDec = valueEnc;

            crypter.DecryptCode(ref addrDec, ref valueDec);

            string actualCode = $"{addrDec:X8} {valueDec:X8}";
            string expectedCode = $"{DecryptedCodes[i][0]:X8} {DecryptedCodes[i][1]:X8}";

            Assert.That(actualCode, Is.EqualTo(expectedCode));
        }
    }

    [Test]
    public void TestEncrypt()
    {
        var crypter = new GbaGsCrypter(true);

        for (int i = 0; i < DecryptedCodes.Length; i++)
        {
            u32 addrDec = DecryptedCodes[i][0];
            u32 valueDec = DecryptedCodes[i][1];

            u32 addrEnc = addrDec;
            u32 valueEnc = valueDec;

            crypter.EncryptCode(ref addrEnc, ref valueEnc);

            string actualCode = $"{addrEnc:X8} {valueEnc:X8}";
            string expectedCode = $"{EncryptedCodes[i][0]:X8} {EncryptedCodes[i][1]:X8}";

            Assert.That(actualCode, Is.EqualTo(expectedCode));
        }
    }
}
