using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.Nintendo64;

namespace LibreShark.Hammerhead.Test.N64;

[TestFixture]
public class EmbeddedFileTest
{
    [Test]
    public void Test_Lzari_Decompression()
    {
        const string romFilePath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        var lzari = new N64GsLzariEncoder();

        List<EmbeddedFile> compressedFiles = rom.EmbeddedFiles;

        Assert.That(compressedFiles, Has.Count.EqualTo(10));

        foreach (EmbeddedFile file in compressedFiles)
        {
            string expectedCompressedFilePath = $"TestData/RomFiles/N64/GsRomSplit/{file.FileName}";
            string expectedUncompressedFilePath = $"TestData/RomFiles/N64/GsRomSplit/{file.FileName}.dec.bin";
            if (!File.Exists(expectedCompressedFilePath) || !File.Exists(expectedUncompressedFilePath))
            {
                continue;
            }

            // Test that the compressed input files are spliced correctly before
            // decompressing.
            u8[] actualCompressed = file.CompressedBytes;
            u8[] expectedCompressed = File.ReadAllBytes(expectedCompressedFilePath);
            Assert.That(actualCompressed, Is.EqualTo(expectedCompressed));

            // Verify that decompression works correctly.
            u8[] actualUncompressed = lzari.Decode(actualCompressed);
            u8[] expectedUncompressed = File.ReadAllBytes(expectedUncompressedFilePath);
            Assert.That(actualUncompressed, Is.EqualTo(expectedUncompressed));
        }
    }

    [Test]
    public void Test_GameShark_V250()
    {
        const string romFilePath = "TestData/RomFiles/N64/gs-2.50-xxxx0504-v3.3-codes.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        Assert.Multiple(() =>
        {
            Assert.That(rom.EmbeddedFiles, Has.Count.EqualTo(9));
            Assert.That(rom.EmbeddedFiles.FindAll(file => file.FileName.Contains("gslogo2")), Has.Count.EqualTo(2));
            Assert.That(rom.EmbeddedImages, Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void Test_GameShark_V330()
    {
        const string romFilePath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        Assert.Multiple(() =>
        {
            Assert.That(rom.EmbeddedFiles, Has.Count.EqualTo(10));
            Assert.That(rom.EmbeddedFiles.FindAll(file => file.FileName.Contains("gslogo3")), Has.Count.EqualTo(2));
            Assert.That(rom.EmbeddedImages, Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void Test_ActionReplay_V330()
    {
        const string romFilePath = "TestData/RomFiles/N64/arpro-3.3-20000418-dirty.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        Assert.Multiple(() =>
        {
            Assert.That(rom.EmbeddedFiles, Has.Count.EqualTo(10));
            Assert.That(rom.EmbeddedFiles.FindAll(file => file.FileName.Contains("arlogo3")), Has.Count.EqualTo(2));
            Assert.That(rom.EmbeddedImages, Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void Test_Equalizer_V300()
    {
        const string romFilePath = "TestData/RomFiles/N64/eq-3.00-19990720-dirty-dump1.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        Assert.Multiple(() =>
        {
            Assert.That(rom.EmbeddedFiles, Has.Count.EqualTo(10));
            Assert.That(rom.EmbeddedFiles.FindAll(file => file.FileName.Contains("equal3")), Has.Count.EqualTo(2));
            Assert.That(rom.EmbeddedImages, Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void Test_GameBuster_V321()
    {
        const string romFilePath = "TestData/RomFiles/N64/gb-3.21-19990805-dirty.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        Assert.Multiple(() =>
        {
            Assert.That(rom.EmbeddedFiles, Has.Count.EqualTo(10));
            Assert.That(rom.EmbeddedFiles.FindAll(file => file.FileName.Contains("gblogo3")), Has.Count.EqualTo(2));
            Assert.That(rom.EmbeddedImages, Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void Test_LibreShark_V4XX()
    {
        const string romFilePath = "TestData/RomFiles/N64/libreshark-pro-v4.05-20230710-mario.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        Assert.Multiple(() =>
        {
            Assert.That(rom.EmbeddedFiles, Has.Count.EqualTo(10));
            Assert.That(rom.EmbeddedFiles.FindAll(file => file.FileName.Contains("lslogo4")), Has.Count.EqualTo(2));
            Assert.That(rom.EmbeddedImages, Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void Test_StartupLogo()
    {
        var paletteBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3.pal.dec.bin");
        var imageBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3.bin.dec.bin");
        var decoder = new N64GsImageEncoder();
        using Image<Rgba32> image = decoder.DecodeStartupLogo(paletteBytes, imageBytes);
        image.SaveAsPng("TestData/RomFiles/N64/GsRomSplit/gslogo3-extracted.png");
        u8[] expectedBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3.png");
        u8[] actualBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3-extracted.png");
        Assert.That(actualBytes, Is.EqualTo(expectedBytes));
    }

    [Test]
    public void Test_StartupTile()
    {
        var imageBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/tile1.tg~.dec.bin");
        var decoder = new N64GsImageEncoder();
        using Image<Rgba32> image = decoder.DecodeTileGraphic(imageBytes);
        image.SaveAsPng("TestData/RomFiles/N64/GsRomSplit/tile1-extracted.png");
        u8[] expectedBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/tile1.png");
        u8[] actualBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/tile1-extracted.png");
        Assert.That(actualBytes, Is.EqualTo(expectedBytes));
    }

    [Test]
    public void Test_Write_StartupLogo_ReencodeExisting()
    {
        var encoder = new N64GsImageEncoder();
        Image<Rgba32> inputPng = Image.Load<Rgba32>("TestData/RomFiles/N64/GsRomSplit/gslogo3.png");
        (u8[] paletteBytes, u8[] dataBytes) = encoder.EncodeStartupLogo(inputPng);
        Image<Rgba32> outputPng = encoder.DecodeStartupLogo(paletteBytes, dataBytes);
        outputPng.SaveAsPng("TestData/RomFiles/N64/GsRomSplit/gslogo3-test.png");
        u8[] expectedPngBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3.png");
        u8[] actualPngBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3-test.png");
        Assert.That(actualPngBytes, Is.EqualTo(expectedPngBytes));
    }

    [Test]
    public void Test_Write_StartupLogo_Quantization_ReduceColorPalette()
    {
        var encoder = new N64GsImageEncoder();
        Image<Rgba32> inputPng = Image.Load<Rgba32>("TestData/RomFiles/N64/GsRomSplit/libreshark-logo-full-color.png");
        (u8[] paletteBytes, u8[] dataBytes) = encoder.EncodeStartupLogo(inputPng);
        Image<Rgba32> outputPng = encoder.DecodeStartupLogo(paletteBytes, dataBytes);
        outputPng.SaveAsPng("TestData/RomFiles/N64/GsRomSplit/libreshark-logo-actual.png");
        u8[] expectedPngBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/libreshark-logo-expected.png");
        u8[] actualPngBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/libreshark-logo-actual.png");
        Assert.That(actualPngBytes, Is.EqualTo(expectedPngBytes));
    }
}
