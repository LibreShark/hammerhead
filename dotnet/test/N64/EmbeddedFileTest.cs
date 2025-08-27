using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.Nintendo64;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
            string expectedCompressedFilePath = $"TestData/RomFiles/N64/gspro-3.30-20000404-pristine/{file.FileName}";
            string expectedUncompressedFilePath = $"TestData/RomFiles/N64/gspro-3.30-20000404-pristine/{file.FileName}.dec.bin";
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
    public void Test_Read_GameShark_V250()
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
    public void Test_Read_GameShark_V330()
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
    public void Test_Read_ActionReplay_V330()
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
    public void Test_Read_Equalizer_V300()
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
    public void Test_Read_GameBuster_V321()
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
    public void Test_Read_LibreShark_V4XX()
    {
        const string romFilePath = "TestData/RomFiles/N64/libreshark-pro-v4.01-20230714-mario.bin";
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
    public void Test_Read_StartupLogo()
    {
        const string romPath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine.bin";
        const string actualPngPath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine/gslogo3-actual.png";
        const string expectedPngPath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine/gslogo3.png";

        var gsRom = N64GsRom.Create(romPath, File.ReadAllBytes(romPath));
        gsRom.StartupLogo?.SaveAsPng(actualPngPath);

        List<Rgba32> expectedPixels = GetPixels(expectedPngPath);
        List<Rgba32> actualPixels = GetPixels(actualPngPath);
        Assert.That(actualPixels, Is.EqualTo(expectedPixels));
    }

    [Test]
    public void Test_Read_StartupTile()
    {
        const string romPath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine.bin";
        const string actualPngPath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine/tile1-actual.png";
        const string expectedPngPath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine/tile1.png";

        var gsRom = N64GsRom.Create(romPath, File.ReadAllBytes(romPath));
        gsRom.StartupTile?.SaveAsPng(actualPngPath);

        List<Rgba32> expectedPixels = GetPixels(expectedPngPath);
        List<Rgba32> actualPixels = GetPixels(actualPngPath);
        Assert.That(actualPixels, Is.EqualTo(expectedPixels));
    }

    [Test]
    public void Test_Write_StartupLogo_ReencodeExisting()
    {
        const string expectedPngPath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine/gslogo3.png";
        const string actualPngPath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine/gslogo3-actual.png";

        var imageEncoder = new N64GsImageEncoder();

        Image<Rgba32> inputPng = Image.Load<Rgba32>(expectedPngPath);
        (u8[] paletteBytes, u8[] dataBytes) = imageEncoder.EncodeStartupLogo(inputPng);
        Image<Rgba32> outputPng = imageEncoder.DecodeStartupLogo(paletteBytes, dataBytes);
        outputPng.SaveAsPng(actualPngPath);

        string cwd = Path.GetDirectoryName(typeof(EmbeddedFileTest).Assembly.Location) ?? "";

        List<Rgba32> expectedPixels = GetPixels(inputPng);
        List<Rgba32> actualPixels = GetPixels(outputPng);
        string expectedMsg = $"Expected: {cwd}/{expectedPngPath}";
        string actualMsg = $"Actual:   {cwd}/{actualPngPath}";
        Assert.That(actualPixels, Is.EqualTo(expectedPixels), $"\n\n{expectedMsg}\n{actualMsg}\n");
    }

    [Test]
    public void Test_Write_StartupLogo_Quantization_ReduceColorPalette()
    {
        const string romPath = "TestData/RomFiles/N64/libreshark-pro-v4.01-20230714-mario.bin";
        const string fullColorPath = "TestData/RomFiles/N64/libreshark-pro-v4.01-20230714-mario/lslogo4-full-color.png";
        const string actualReducedColorPath = "TestData/RomFiles/N64/libreshark-pro-v4.01-20230714-mario/lslogo4-actual.png";
        const string expectedReducedColorPath = "TestData/RomFiles/N64/libreshark-pro-v4.01-20230714-mario/lslogo4.png";

        var gsRom = N64GsRom.Create(romPath, File.ReadAllBytes(romPath));
        gsRom.SetStartupLogo(Image.Load<Rgba32>(fullColorPath));
        gsRom.StartupLogo?.SaveAsPng(actualReducedColorPath);

        string cwd = Path.GetDirectoryName(typeof(EmbeddedFileTest).Assembly.Location) ?? "";

        List<Rgba32> expectedPixels = GetPixels(expectedReducedColorPath);
        List<Rgba32> actualPixels = GetPixels(actualReducedColorPath);
        string originalMsg = $"Original: {cwd}/{fullColorPath}";
        string expectedMsg = $"Expected: {cwd}/{expectedReducedColorPath}";
        string actualMsg = $"Actual:   {cwd}/{actualReducedColorPath}";
        Assert.That(actualPixels, Is.EqualTo(expectedPixels), $"\n\n{originalMsg}\n{expectedMsg}\n{actualMsg}\n");
    }

    private static List<Rgba32> GetPixels(string imagePath)
    {
        return GetPixels(Image.Load<Rgba32>(imagePath));
    }

    private static List<Rgba32> GetPixels(Image<Rgba32> image)
    {
        var pixels = new List<Rgba32>();
        for (var x = 0; x < image.Width; x++)
        {
            for (var y = 0; y < image.Height; y++)
            {
                // Ensure that fully transparent pixels are comparable, regardless of their "color".
                // E.g., (0, 0, 0, 0) and (0, 0, 0, 255) should be considered equal.
                var pixel = image[x, y];
                if (pixel.A == 0)
                {
                    pixel.R = 0;
                    pixel.G = 0;
                    pixel.B = 0;
                }
                pixels.Add(pixel);
            }
        }
        return pixels;
    }
}
