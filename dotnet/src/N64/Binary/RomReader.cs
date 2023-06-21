// bacteriamage.wordpress.com

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Read the full list of games and cheats from a GameShark ROM image.
/// </summary>
class RomReader : RomBase
{
    public static N64RomInfo? FromFile(string path)
    {
        RomReader reader = new RomReader();
        reader.ReadRomFromFile(path);
        var version = reader.ReadVersion();
        if (version == null) {
            return null;
        }
        return new N64RomInfo(version, reader.ReadGames(), reader.ReadKeyCodes());
    }

    public static N64RomInfo? FromBytes(byte[] bytes)
    {
        RomReader reader = new RomReader();
        reader.ReadRomFromBytes(bytes);
        var version = reader.ReadVersion();
        if (version == null) {
            return null;
        }

        ;
        return new N64RomInfo(
            version,
            reader.ReadGames(),
            reader.ReadKeyCodes(),
            Checksum.From(bytes),
            reader.ReadActiveKeyCode()
                );
    }

    private RomReader()
    {
    }

    private List<N64Game> ReadGames()
    {
        List<N64Game> games = new List<N64Game>();

        SeekGamesList();

        int gamesCount = Reader.ReadSInt32();

        for (int gameIndex = 0; gameIndex < gamesCount; gameIndex++)
        {
            games.Add(ReadGame());
        }

        return games;
    }

    private N64Game ReadGame()
    {
        return GameDecoder.FromReader(Reader);
    }
}
