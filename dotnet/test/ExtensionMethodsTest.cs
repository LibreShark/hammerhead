namespace LibreShark.Hammerhead.Test;

[TestFixture]
public class ExtensionMethodsTest
{
    [Test]
    public void TestToAsciiString()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new u8[] { }.ToAsciiString(), Is.EqualTo(""));
            Assert.That(new u8[] { 0 }.ToAsciiString(), Is.EqualTo("\0"));
            Assert.That(new u8[] { 1 }.ToAsciiString(), Is.EqualTo("\u0001"));
            Assert.That(new u8[] { 127 }.ToAsciiString(), Is.EqualTo("\u007f"));
            Assert.That(new u8[] { 128 }.ToAsciiString(), Is.EqualTo("€"));
            Assert.That(new u8[] { 255 }.ToAsciiString(), Is.EqualTo("ÿ"));
            Assert.That("abcd"u8.ToArray().ToAsciiString(), Is.EqualTo("abcd"));
        });
    }

    [Test]
    public void TestToHexString()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new u8[] { }.ToHexString(), Is.EqualTo(""));
            Assert.That(new u8[] { }.ToHexString(" "), Is.EqualTo(""));
            Assert.That(new u8[] { 0x00, 0x0F, 0x10, 0xFF }.ToHexString(), Is.EqualTo("000F10FF"));
            Assert.That(new u8[] { 0x00, 0x0F, 0x10, 0xFF }.ToHexString(" "), Is.EqualTo("00 0F 10 FF"));
        });
    }

    [Test]
    public void TestToCodeString()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new u8[] { 0x81, 0x25, 0x59, 0x9C, 0x46, 0x1F }.ToCodeString(ConsoleId.Nintendo64), Is.EqualTo("8125599C 461F"));
            Assert.That(new u8[] { 0x81, 0x25, 0x59, 0x9C, 0x46, 0x1F }.ToCodeString(ConsoleId.UnknownConsole), Is.EqualTo("8125599C461F"));
            Assert.That(new u8[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }.ToCodeString(ConsoleId.GameBoyAdvance), Is.EqualTo("01020304 05060708"));
        });
    }

    [Test]
    public void TestIntToEndianBytes()
    {
        const u16 u16 = 0x1234;
        const u32 u32 = 0x12345678;
        const u64 u64 = 0x12345678_90ABCDEF;

        Assert.Multiple(() =>
        {
            Assert.That(u16.ToBigEndianBytes(), Is.EqualTo(new u8[] {0x12, 0x34}));
            Assert.That(u16.ToLittleEndianBytes(), Is.EqualTo(new u8[] {0x34, 0x12}));

            Assert.That(u32.ToBigEndianBytes(), Is.EqualTo(new u8[] {0x12, 0x34, 0x56, 0x78}));
            Assert.That(u32.ToLittleEndianBytes(), Is.EqualTo(new u8[] {0x78, 0x56, 0x34, 0x12}));

            Assert.That(u64.ToBigEndianBytes(), Is.EqualTo(new u8[] {0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF}));
            Assert.That(u64.ToLittleEndianBytes(), Is.EqualTo(new u8[] {0xEF, 0xCD, 0xAB, 0x90, 0x78, 0x56, 0x34, 0x12}));
        });
    }

    [Test]
    public void TestEndianBytesToInt()
    {
        const u16 u16 = 0x1234;
        const u32 u32 = 0x12345678;
        const u64 u64 = 0x12345678_90ABCDEF;

        Assert.Multiple(() =>
        {
            Assert.That(new u8[] {0x12, 0x34}.BigEndianToU16(), Is.EqualTo(u16));
            Assert.That(new u8[] {0x34, 0x12}.LittleEndianToU16(), Is.EqualTo(u16));

            Assert.That(new u8[] {0x12, 0x34, 0x56, 0x78}.BigEndianToU32(), Is.EqualTo(u32));
            Assert.That(new u8[] {0x78, 0x56, 0x34, 0x12}.LittleEndianToU32(), Is.EqualTo(u32));

            Assert.That(new u8[] {0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF}.BigEndianToU64(), Is.EqualTo(u64));
            Assert.That(new u8[] {0xEF, 0xCD, 0xAB, 0x90, 0x78, 0x56, 0x34, 0x12}.LittleEndianToU64(), Is.EqualTo(u64));
        });
    }
}
