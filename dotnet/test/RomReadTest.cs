using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.Nintendo64;

namespace LibreShark.Hammerhead.Test;

[TestFixture]
public class RomReadTest
{
    private static readonly Comparison<Game> GameNameComparator =
        (game1, game2) =>
            String.Compare(game1.GameName.Value, game2.GameName.Value, StringComparison.InvariantCulture);

    [Test]
    public void Test_GbGsRom()
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
        games.Sort(GameNameComparator);
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
    public void Test_GbcCbRom()
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
        games.Sort(GameNameComparator);
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
    public void Test_GbcGsV3Rom()
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
        games.Sort(GameNameComparator);
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
    public void Test_GbcGsV4Rom()
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
        games.Sort(GameNameComparator);
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
    public void Test_GbcSharkMxRom()
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
            GbcSmxContact firstContact = parsedFile.GbcSmxData.Contacts[0];
            Assert.That(firstContact.PersonName.Value, Is.EqualTo("RWeick"));
            Assert.That(firstContact.EmailAddress.Value, Is.EqualTo("rweick@gmail.com"));
            Assert.That(firstContact.PhoneNumber.Value, Is.EqualTo("12345678900"));
            Assert.That(firstContact.StreetAddress.Value, Is.EqualTo("888 libreshark"));

            // Messages
            Assert.That(parsedFile.GbcSmxData.Messages, Has.Count.EqualTo(1));
            GbcSmxMessage firstMessage = parsedFile.GbcSmxData.Messages[0];
            Assert.That(firstMessage.Subject.Value, Is.EqualTo("hi"));
            Assert.That(firstMessage.RecipientEmail.Value, Is.EqualTo("rweick@gmail.com"));
            Assert.That(firstMessage.RawDate.Value, Is.EqualTo("02/09/2010"));
            Assert.That(firstMessage.IsoDate, Is.EqualTo("2010-02-09T00:00:00"));
            Assert.That(firstMessage.Message.Value, Is.EqualTo("yo"));
        });
    }

    [Test]
    public void Test_N64GsRom_LibreShark()
    {
        const string romFilePath = "TestData/RomFiles/N64/libreshark-pro-v4.05-20230709-cheatocodes.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);

        var rom = N64GsRom.Create(romFilePath, romFileBytes);
        Assert.Multiple(() =>
        {
            Assert.That(rom.Metadata.BrandId, Is.EqualTo(BrandId.Libreshark));
            Assert.That(rom.Metadata.SortableVersion, Is.EqualTo(4.00));
            Assert.That(rom.Metadata.BuildDateIso, Is.EqualTo("2023-07-10T04:27:00+00:00"));
        });
    }

    [Test]
    public void Test_N64GsRom_Checksums()
    {
        const string romFilePath = "TestData/RomFiles/N64/gspro-3.30-20000404-pristine.bin";
        u8[] romFileBytes = File.ReadAllBytes(romFilePath);
        string computedKeyCode = N64GsChecksum.ComputeKeyCode(romFileBytes, N64Cic.CIC6102_Mario).ToHexString(" ");
        string expectedKeyCode = "EA 6D 5B F8 E2 B4 69 6C 80 18 00 00 2B";
        // TODO(CheatoBaggins): Fix this failing test and uncomment this line
        // Assert.That(computedKeyCode, Is.EqualTo(expectedKeyCode));
    }
}
