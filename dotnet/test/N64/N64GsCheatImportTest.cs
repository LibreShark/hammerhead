using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.Nintendo64;

namespace LibreShark.Hammerhead.Test.N64;

[TestFixture]
public class N64GsCheatImportTest
{
  private const string RomFilePath =
      "TestData/RomFiles/N64/gspro-3.30-20000404-pristine.bin";
  private const string CheatsFilePath =
      "TestData/CheatFiles/n64-datel-v3.30-custom-cheats.txt";

  [Test]
  public void Test_CopyCheats_DatelTextToExistingRom_PreservesRomState()
  {
    ICodec source = AbstractCodec.ReadFromFile(CheatsFilePath);
    var destination = (N64GsRom)AbstractCodec.ReadFromFile(RomFilePath);
    List<Game> sourceGames = source.Games.ToList();
    List<Game> destinationGames = destination.Games.ToList();
    var selectedGame = destinationGames
        .Select((game, destinationIndex) => new
        {
          Game = game,
          DestinationIndex = destinationIndex,
          SourceIndex = sourceGames.FindIndex(sourceGame =>
                  sourceGame.GameName.Value.Equals(
                      game.GameName.Value,
                      StringComparison.OrdinalIgnoreCase)),
        })
        .First(candidate =>
            candidate.SourceIndex >= 0 &&
            candidate.SourceIndex != candidate.DestinationIndex);
    string removedGameName = destinationGames
        .Select(game => game.GameName.Value)
        .First(name => !sourceGames.Any(sourceGame =>
            sourceGame.GameName.Value.Equals(name, StringComparison.OrdinalIgnoreCase)));

    destination.UpdateUserPrefs(new N64GsConfigureCmdParams()
    {
      SelectedGame = selectedGame.Game.GameName.Value,
      IsSoundEnabled = false,
      IsBgScrollEnabled = false,
      IsMenuScrollEnabled = false,
      BgPattern = N64GsBgPatternId.Logo,
      BgColor = N64GsBgColorId.Blue,
    });
    destination.WriteChangesToBuffer();

    string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"hammerhead-{Guid.NewGuid():N}");
    string outputFilePath = Path.Combine(tempDirectory, "gspro-3.30-imported.bin");
    Directory.CreateDirectory(tempDirectory);

    try
    {
      File.WriteAllBytes(outputFilePath, destination.Buffer);
      var before = (N64GsRom)AbstractCodec.ReadFromFile(outputFilePath);
      ParsedFile beforeParsed = before.ToFullProto();

      new HammerheadApi().CopyCheats(new RomCmdParams()
      {
        InputFile = new FileInfo(CheatsFilePath),
        OutputFile = new FileInfo(outputFilePath),
        OutputFormat = CodecId.Auto,
        OverwriteExistingFiles = true,
        PrintFormatId = PrintFormatId.Plain,
        HideBanner = true,
      });

      var after = (N64GsRom)AbstractCodec.ReadFromFile(outputFilePath);
      ParsedFile afterParsed = after.ToFullProto();

      AssertCheatTreesEqual(sourceGames, after.Games);
      Assert.Multiple(() =>
      {
        Assert.That(destinationGames, Has.Count.Not.EqualTo(sourceGames.Count));
        Assert.That(beforeParsed.N64Data.GsUserPrefs.SelectedGameIndex,
                  Is.EqualTo(selectedGame.DestinationIndex));
        Assert.That(after.Games.Any(game => game.GameName.Value == removedGameName), Is.False);
        Assert.That(after.Buffer, Has.Length.EqualTo(before.Buffer.Length));

        Assert.That(after.Metadata.CodecId, Is.EqualTo(before.Metadata.CodecId));
        Assert.That(after.Metadata.ConsoleId, Is.EqualTo(before.Metadata.ConsoleId));
        Assert.That(after.Metadata.BrandId, Is.EqualTo(before.Metadata.BrandId));
        Assert.That(after.Metadata.DisplayVersion, Is.EqualTo(before.Metadata.DisplayVersion));
        Assert.That(after.Metadata.SortableVersion, Is.EqualTo(before.Metadata.SortableVersion));
        Assert.That(after.Metadata.BuildDateRaw.Value, Is.EqualTo(before.Metadata.BuildDateRaw.Value));
        Assert.That(after.Metadata.BuildDateIso, Is.EqualTo(before.Metadata.BuildDateIso));
        Assert.That(after.Metadata.LanguageIetfCode, Is.EqualTo(before.Metadata.LanguageIetfCode));
        Assert.That(after.Metadata.TvStandard, Is.EqualTo(before.Metadata.TvStandard));
        Assert.That(after.IsFirmwareCompressed(), Is.EqualTo(before.IsFirmwareCompressed()));
        Assert.That(after.IsFileEncrypted(), Is.EqualTo(before.IsFileEncrypted()));

        N64GsUserPrefs beforePrefs = beforeParsed.N64Data.GsUserPrefs;
        N64GsUserPrefs afterPrefs = afterParsed.N64Data.GsUserPrefs;
        Assert.That(afterPrefs.IsSoundEnabled, Is.EqualTo(beforePrefs.IsSoundEnabled));
        Assert.That(afterPrefs.IsBgScrollEnabled, Is.EqualTo(beforePrefs.IsBgScrollEnabled));
        Assert.That(afterPrefs.IsMenuScrollEnabled, Is.EqualTo(beforePrefs.IsMenuScrollEnabled));
        Assert.That(afterPrefs.BgPatternId, Is.EqualTo(beforePrefs.BgPatternId));
        Assert.That(afterPrefs.BgColorId, Is.EqualTo(beforePrefs.BgColorId));
        Assert.That(afterPrefs.SelectedGameIndex, Is.EqualTo(selectedGame.SourceIndex));
        Assert.That(after.Games.Count(game => game.IsGameActive), Is.EqualTo(1));
        Assert.That(after.Games[selectedGame.SourceIndex].GameName.Value,
                  Is.EqualTo(selectedGame.Game.GameName.Value));
        Assert.That(after.Games[selectedGame.SourceIndex].IsGameActive, Is.True);
      });

      AssertKeyCodesEqual(beforeParsed.N64Data.KeyCodes, afterParsed.N64Data.KeyCodes);
    }
    finally
    {
      Directory.Delete(tempDirectory, true);
    }
  }

  [Test]
  public void Test_ImportCheats_SelectedGameMissing_ClearsSelection()
  {
    ICodec source = AbstractCodec.ReadFromFile(CheatsFilePath);
    var destination = (N64GsRom)AbstractCodec.ReadFromFile(RomFilePath);
    var sourceGameNames = source.Games
        .Select(game => game.GameName.Value)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    Game missingGame = destination.Games.First(game =>
        !sourceGameNames.Contains(game.GameName.Value));
    destination.UpdateUserPrefs(new N64GsConfigureCmdParams()
    {
      SelectedGame = missingGame.GameName.Value,
    });

    destination.ImportCheats(source.Games);

    ParsedFile imported = destination.ToFullProto();
    Assert.Multiple(() =>
    {
      Assert.That(imported.N64Data.GsUserPrefs.SelectedGameIndex, Is.EqualTo(-1));
      Assert.That(imported.Games.Any(game => game.IsGameActive), Is.False);
      Assert.That(source.Games.Any(game => game.IsGameActive), Is.False);
    });
  }

  [Test]
  public void Test_CopyCheats_DatelTextToJson_PreservesSourceContext()
  {
    ICodec source = AbstractCodec.ReadFromFile(CheatsFilePath);
    string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"hammerhead-{Guid.NewGuid():N}");
    string outputFilePath = Path.Combine(tempDirectory, "cheats.json");
    Directory.CreateDirectory(tempDirectory);

    try
    {
      new HammerheadApi().CopyCheats(new RomCmdParams()
      {
        InputFile = new FileInfo(CheatsFilePath),
        OutputFile = new FileInfo(outputFilePath),
        OutputFormat = CodecId.HammerheadJson,
        OverwriteExistingFiles = true,
        PrintFormatId = PrintFormatId.Plain,
        HideBanner = true,
      });

      ICodec output = AbstractCodec.ReadFromFile(outputFilePath);
      AssertCheatTreesEqual(source.Games, output.Games);
      Assert.Multiple(() =>
      {
        Assert.That(output.Metadata.CodecId, Is.EqualTo(CodecId.HammerheadJson));
        Assert.That(output.Metadata.ConsoleId, Is.EqualTo(source.Metadata.ConsoleId));
        Assert.That(output.Metadata.BrandId, Is.EqualTo(source.Metadata.BrandId));
      });
    }
    finally
    {
      Directory.Delete(tempDirectory, true);
    }
  }

  private static void AssertCheatTreesEqual(
      IEnumerable<Game> expectedGames,
      IEnumerable<Game> actualGames)
  {
    List<Game> expected = expectedGames.ToList();
    List<Game> actual = actualGames.ToList();
    Assert.That(actual, Has.Count.EqualTo(expected.Count));

    for (int gameIndex = 0; gameIndex < expected.Count; gameIndex++)
    {
      Game expectedGame = expected[gameIndex];
      Game actualGame = actual[gameIndex];
      Assert.That(actualGame.GameName.Value, Is.EqualTo(expectedGame.GameName.Value));
      Assert.That(actualGame.Cheats, Has.Count.EqualTo(expectedGame.Cheats.Count));

      for (int cheatIndex = 0; cheatIndex < expectedGame.Cheats.Count; cheatIndex++)
      {
        Cheat expectedCheat = expectedGame.Cheats[cheatIndex];
        Cheat actualCheat = actualGame.Cheats[cheatIndex];
        Assert.That(actualCheat.CheatName.Value, Is.EqualTo(expectedCheat.CheatName.Value));
        Assert.That(actualCheat.IsCheatActive, Is.EqualTo(expectedCheat.IsCheatActive));
        Assert.That(actualCheat.Codes, Has.Count.EqualTo(expectedCheat.Codes.Count));

        for (int codeIndex = 0; codeIndex < expectedCheat.Codes.Count; codeIndex++)
        {
          Assert.That(
              actualCheat.Codes[codeIndex].Bytes,
              Is.EqualTo(expectedCheat.Codes[codeIndex].Bytes));
        }
      }
    }
  }

  private static void AssertKeyCodesEqual(
      IEnumerable<Code> expectedKeyCodes,
      IEnumerable<Code> actualKeyCodes)
  {
    List<Code> expected = expectedKeyCodes.ToList();
    List<Code> actual = actualKeyCodes.ToList();
    Assert.That(actual, Has.Count.EqualTo(expected.Count));

    for (int keyCodeIndex = 0; keyCodeIndex < expected.Count; keyCodeIndex++)
    {
      Code expectedKeyCode = expected[keyCodeIndex];
      Code actualKeyCode = actual[keyCodeIndex];
      Assert.Multiple(() =>
      {
        Assert.That(actualKeyCode.CodeName.Value, Is.EqualTo(expectedKeyCode.CodeName.Value));
        Assert.That(actualKeyCode.IsActiveKeyCode, Is.EqualTo(expectedKeyCode.IsActiveKeyCode));
        Assert.That(
                  actualKeyCode.Bytes.ToByteArray()[8..12],
                  Is.EqualTo(expectedKeyCode.Bytes.ToByteArray()[8..12]));
      });
    }
  }
}
