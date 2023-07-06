using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Codecs;

namespace LibreShark.Hammerhead.Test;

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

        Game? activeGame = games.FirstOrDefault(game => game.IsGameActive);

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
}
