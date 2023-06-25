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
    [Test]
    public void TestDecrypt()
    {
        u32 addr1Enc = 0xD925CBB3;
        u32 value1Enc = 0x457D65D3;

        u32 addr2Enc = 0xB6FA891B;
        u32 value2Enc = 0xDC4362A9;

        u32 addr1Dec = addr1Enc;
        u32 value1Dec = value1Enc;

        u32 addr2Dec = addr2Enc;
        u32 value2Dec = value2Enc;

        var crypter = new GbaGsCrypter(true);

        crypter.DecryptCode(ref addr1Dec, ref value1Dec);
        crypter.DecryptCode(ref addr2Dec, ref value2Dec);

        Assert.Multiple(() =>
        {
            const string expectedCode1 = "C4000970 00008401";
            const string expectedCode2 = "45584D42 001DC0DE";

            string actualCode1 = $"{addr1Dec:X8} {value1Dec:X8}";
            string actualCode2 = $"{addr2Dec:X8} {value2Dec:X8}";

            Assert.That(actualCode1, Is.EqualTo(expectedCode1));
            Assert.That(actualCode2, Is.EqualTo(expectedCode2));
        });
    }
}
