using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.Nintendo64;

namespace LibreShark.Hammerhead.Test;

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
public class CodecTest
{
    [Test]
    public void Test_GbGsV2_Read()
    {
        var api = new HammerheadApi();
        List<ICodec> codecs = api.ParseFiles(new InfoCmdParams()
        {
            InputFiles = new FileInfo[]
            {
                new FileInfo("TestData/RomFiles/GB/gb-gs-v2.1-1997-NM27C010-PLCC32.gb"),
            },
        });

        Assert.That(codecs, Has.Count.EqualTo(1));

        ICodec codec = codecs.First();

        List<Game> games = codec.Games.ToList();
        games.Sort((game1, game2) => String.Compare(game1.GameName.Value, game2.GameName.Value, StringComparison.InvariantCulture));
        List<Cheat> cheats = games.SelectMany(game => game.Cheats).ToList();
        List<Code> codes = cheats.SelectMany(cheat => cheat.Codes).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(codec.Metadata.CodecId, Is.EqualTo(CodecId.GbGamesharkRom));
            Assert.That(codec.Metadata.ConsoleId, Is.EqualTo(ConsoleId.GameBoyOriginal));
            Assert.That(codec.Metadata.BrandId, Is.EqualTo(BrandId.Gameshark));
            Assert.That(codec.Metadata.DisplayVersion, Is.EqualTo("v1.04"));
            Assert.That(codec.Metadata.BuildDateRaw.Value, Is.EqualTo("97"));

            Assert.That(games, Has.Count.EqualTo(155));
            Assert.That(cheats, Has.Count.EqualTo(250));
            Assert.That(codes, Has.Count.EqualTo(250));

            Assert.That(games[1].GameName.Value, Is.EqualTo("ALADDIN"));
            Assert.That(games[1].Cheats[0].CheatName.Value, Is.EqualTo("Inf Lives"));
            Assert.That(games[1].Cheats[0].Codes[0].Formatted, Is.EqualTo("010309DC"));

            Assert.That(games[149].GameName.Value, Is.EqualTo("WARIO LAND"));
            Assert.That(games[149].Cheats[0].CheatName.Value, Is.EqualTo("Unl Time"));
            Assert.That(games[149].Cheats[0].Codes[0].Formatted, Is.EqualTo("809965A9"));
        });
    }

    [Test]
    public void Test_GbcCbRom_Read()
    {
        var api = new HammerheadApi();
        List<ICodec> codecs = api.ParseFiles(new InfoCmdParams()
        {
            InputFiles = new FileInfo[]
            {
                new FileInfo("TestData/RomFiles/GBC/gbc-codebreaker-v1.0c-SST29EE020PLCC32.gbc"),
            },
        });

        Assert.That(codecs, Has.Count.EqualTo(1));

        ICodec codec = codecs.First();

        List<Game> games = codec.Games.ToList();
        games.Sort((game1, game2) => String.Compare(game1.GameName.Value, game2.GameName.Value, StringComparison.InvariantCulture));
        List<Cheat> cheats = games.SelectMany(game => game.Cheats).ToList();
        List<Code> codes = cheats.SelectMany(cheat => cheat.Codes).ToList();

        Game? activeGame = games.FirstOrDefault(game => game.IsGameActive);

        Assert.Multiple(() =>
        {
            Assert.That(codec.Metadata.CodecId, Is.EqualTo(CodecId.GbcCodebreakerRom));
            Assert.That(codec.Metadata.ConsoleId, Is.EqualTo(ConsoleId.GameBoyColor));
            Assert.That(codec.Metadata.BrandId, Is.EqualTo(BrandId.CodeBreaker));
            Assert.That(codec.Metadata.DisplayVersion, Is.EqualTo("v1.0c"));
            Assert.That(activeGame, Is.Not.Null);
            Assert.That(activeGame!.GameName.Value, Is.EqualTo("POKEMON YELLOW"));

            Assert.That(games, Has.Count.EqualTo(240));
            Assert.That(cheats, Has.Count.EqualTo(563));
            Assert.That(codes, Has.Count.EqualTo(694));

            Assert.That(games[0].GameName.Value, Is.EqualTo("ADDAMS FAMILY"));
            Assert.That(games[0].Cheats[0].CheatName.Value, Is.EqualTo("Lives"));
            Assert.That(games[0].Cheats[0].Codes[0].Formatted, Is.EqualTo("00C06505"));

            Assert.That(games[237].GameName.Value, Is.EqualTo("YOGIS GOLDRUSH"));
            Assert.That(games[237].Cheats[1].CheatName.Value, Is.EqualTo("Health"));
            Assert.That(games[237].Cheats[1].Codes[2].Formatted, Is.EqualTo("00C2EEE5"));

            Assert.That(games[238].GameName.Value, Is.EqualTo("ZELDA"));
            Assert.That(games[238].Cheats[1].CheatName.Value, Is.EqualTo("Invincib"));
            Assert.That(games[238].Cheats[1].Codes[0].Formatted, Is.EqualTo("01DBC722"));
        });
    }

    [Test]
    public void Test_GbcGsV3Rom_Read()
    {
        var api = new HammerheadApi();
        List<ICodec> codecs = api.ParseFiles(new InfoCmdParams()
        {
            InputFiles = new FileInfo[]
            {
                new FileInfo("TestData/RomFiles/GBC/gbc-gspro-v3.0-sticker-working.gbc"),
            },
        });

        Assert.That(codecs, Has.Count.EqualTo(1));

        ICodec codec = codecs.First();

        List<Game> games = codec.Games.ToList();
        games.Sort((game1, game2) => String.Compare(game1.GameName.Value, game2.GameName.Value, StringComparison.InvariantCulture));
        List<Cheat> cheats = games.SelectMany(game => game.Cheats).ToList();
        List<Code> codes = cheats.SelectMany(cheat => cheat.Codes).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(codec.Metadata.CodecId, Is.EqualTo(CodecId.GbcGamesharkV3Rom));
            Assert.That(codec.Metadata.ConsoleId, Is.EqualTo(ConsoleId.GameBoyColor));
            Assert.That(codec.Metadata.BrandId, Is.EqualTo(BrandId.Gameshark));
            Assert.That(codec.Metadata.DisplayVersion, Is.EqualTo("v3.00"));

            Assert.That(games, Has.Count.EqualTo(224));
            Assert.That(cheats, Has.Count.EqualTo(377));
            Assert.That(codes, Has.Count.EqualTo(399));

            Assert.That(games[1].GameName.Value, Is.EqualTo("Aladdin"));
            Assert.That(games[1].Cheats[1].CheatName.Value, Is.EqualTo("Inf Lives"));
            Assert.That(games[1].Cheats[1].Codes[0].Formatted, Is.EqualTo("010309DC"));

            Assert.That(games[223].GameName.Value, Is.EqualTo("Wario Land II"));
            Assert.That(games[223].Cheats[0].CheatName.Value, Is.EqualTo("Quick Coins"));
            Assert.That(games[223].Cheats[0].Codes[1].Formatted, Is.EqualTo("01990FD5"));
        });
    }

    [Test]
    public void Test_GbcGsV4Rom_Read()
    {
        var api = new HammerheadApi();
        List<ICodec> codecs = api.ParseFiles(new InfoCmdParams()
        {
            InputFiles = new FileInfo[]
            {
                new FileInfo("TestData/RomFiles/GBC/gbc-gs-v4.0.gbc"),
            },
        });

        Assert.That(codecs, Has.Count.EqualTo(1));

        ICodec codec = codecs.First();

        List<Game> games = codec.Games.ToList();
        games.Sort((game1, game2) => String.Compare(game1.GameName.Value, game2.GameName.Value, StringComparison.InvariantCulture));
        List<Cheat> cheats = games.SelectMany(game => game.Cheats).ToList();
        List<Code> codes = cheats.SelectMany(cheat => cheat.Codes).ToList();

        Game? activeGame = games.FirstOrDefault(game => game.IsGameActive);

        Assert.Multiple(() =>
        {
            Assert.That(codec.Metadata.CodecId, Is.EqualTo(CodecId.GbcGamesharkV4Rom));
            Assert.That(codec.Metadata.ConsoleId, Is.EqualTo(ConsoleId.GameBoyColor));
            Assert.That(codec.Metadata.BrandId, Is.EqualTo(BrandId.Gameshark));
            Assert.That(codec.Metadata.DisplayVersion, Is.EqualTo("v4.00"));

            Assert.That(activeGame, Is.Not.Null);
            Assert.That(activeGame?.GameName.Value, Is.EqualTo("Spider Man"));

            Assert.That(games, Has.Count.EqualTo(283));
            Assert.That(cheats, Has.Count.EqualTo(947));
            Assert.That(codes, Has.Count.EqualTo(1307));

            Assert.That(games[0].GameName.Value, Is.EqualTo("102 Dalmations"));
            Assert.That(games[0].Cheats[0].CheatName.Value, Is.EqualTo("Max Score*"));
            Assert.That(games[0].Cheats[0].Codes[1].Formatted, Is.EqualTo("0199D9C0"));

            Assert.That(games[280].GameName.Value, Is.EqualTo("X-Treme Sports"));
            Assert.That(games[280].Cheats[2].CheatName.Value, Is.EqualTo("Max Points*"));
            Assert.That(games[280].Cheats[2].Codes[4].Formatted, Is.EqualTo("0109FBC8"));
        });
    }

    [Test]
    public void Test_GbcSharkMxRom_Read()
    {
        var api = new HammerheadApi();
        List<ICodec> codecs = api.ParseFiles(new InfoCmdParams()
        {
            InputFiles = new FileInfo[]
            {
                new FileInfo("TestData/RomFiles/GBC/gbc-shark-mx-v1.02-2000-dirty-SST39SF020PLCC32.gbc"),
            },
        });

        Assert.That(codecs, Has.Count.EqualTo(1));

        ICodec codec = codecs.First();

        List<Game> games = codec.Games.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(codec.Metadata.CodecId, Is.EqualTo(CodecId.GbcSharkMxRom));
            Assert.That(codec.Metadata.ConsoleId, Is.EqualTo(ConsoleId.GameBoyColor));
            Assert.That(codec.Metadata.BrandId, Is.EqualTo(BrandId.SharkMx));
            Assert.That(codec.Metadata.DisplayVersion, Is.EqualTo("v1.02 (US)"));

            Assert.That(games, Has.Count.EqualTo(0));

            ParsedFile parsedFile = codec.ToSlimProto();
            Assert.That(parsedFile.GbcSmxData, Is.Not.Null);

            // Registration codes
            Assert.That(parsedFile.GbcSmxData.RegCode1.Value, Is.EqualTo("SHGGGGGGGGGGGGGQ"));
            Assert.That(parsedFile.GbcSmxData.RegCode2.Value, Is.EqualTo("SHGGGGGGGGGGGGGQ"));
            Assert.That(parsedFile.GbcSmxData.SecretPin.Value, Is.EqualTo("1234"));

            // Time zones
            Assert.That(parsedFile.GbcSmxData.Timezones, Has.Count.EqualTo(41));

            // Contacts
            Assert.That(parsedFile.GbcSmxData.Contacts, Has.Count.EqualTo(51));
            Assert.That(parsedFile.GbcSmxData.Contacts[0].PersonName.Value, Is.EqualTo("RWeick"));
            Assert.That(parsedFile.GbcSmxData.Contacts[0].EmailAddress.Value, Is.EqualTo("rweick@gmail.com"));
            Assert.That(parsedFile.GbcSmxData.Contacts[0].PhoneNumber.Value, Is.EqualTo("12345678900"));
            Assert.That(parsedFile.GbcSmxData.Contacts[0].StreetAddress.Value, Is.EqualTo("888 libreshark"));

            // Messages
            Assert.That(parsedFile.GbcSmxData.Messages, Has.Count.EqualTo(1));
            Assert.That(parsedFile.GbcSmxData.Messages[0].Subject.Value, Is.EqualTo("hi"));
            Assert.That(parsedFile.GbcSmxData.Messages[0].RecipientEmail.Value, Is.EqualTo("rweick@gmail.com"));
            Assert.That(parsedFile.GbcSmxData.Messages[0].RawDate.Value, Is.EqualTo("02/09/2010"));
            Assert.That(parsedFile.GbcSmxData.Messages[0].IsoDate, Is.EqualTo("2010-02-09T00:00:00"));
            Assert.That(parsedFile.GbcSmxData.Messages[0].Message.Value, Is.EqualTo("yo"));
        });
    }

    [Test]
    public void Test_N64GsRom_Lzari()
    {
        const string romFilePath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        var lzari = new N64GsLzariEncoder();

        List<CompressedFile> compressedFiles = rom.CompressedFiles;

        Assert.That(compressedFiles, Has.Count.EqualTo(5));

        foreach (CompressedFile file in compressedFiles)
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
    public void Test_N64GsRom_LibreShark()
    {
        const string romFilePath = "TestData/RomFiles/N64/libreshark-pro-v4.02-20230708-cheato.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        Assert.Multiple(() =>
        {
            Assert.That(rom.Metadata.BrandId, Is.EqualTo(BrandId.Libreshark));
            Assert.That(rom.Metadata.SortableVersion, Is.EqualTo(4.00));
            Assert.That(rom.Metadata.BuildDateIso, Is.EqualTo("2023-07-08T08:47:00+00:00"));
        });
    }

    [Test]
    public void Test_N64GsRom_ImageDecoder()
    {
        var paletteBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3.pal.dec.bin");
        var imageBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3.bin.dec.bin");
        var decoder = new N64GsLogoDecoder();
        using Image<Rgba32> image = decoder.Decode(paletteBytes, imageBytes, true, new Rgb24(0, 0, 0));
        image.SaveAsPng("TestData/RomFiles/N64/GsRomSplit/gslogo3-extracted.png");
        u8[] expectedBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3.png");
        u8[] actualBytes = File.ReadAllBytes("TestData/RomFiles/N64/GsRomSplit/gslogo3-extracted.png");
        Assert.That(actualBytes, Is.EqualTo(expectedBytes));
    }
}
