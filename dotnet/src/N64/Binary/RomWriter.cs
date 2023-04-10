// bacteriamage.wordpress.com

using System.Collections.Generic;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Write the full list of games and cheats to a GameShark ROM image.
/// </summary>
class RomWriter : RomBase
{
    public static void ToFile(ICollection<Game> games, string path)
    {
        ToFile(games, path, path);
    }

    public static void ToFile(ICollection<Game> games, string source, string target)
    {
        RomWriter writer = new RomWriter();

        writer.ReadRomFromFile(source);
        writer.WriteGames(games);
        writer.WriteRomToFile(target);
    }

    private RomWriter()
    {
    }

    private void WriteGames(ICollection<Game> games)
    {
        SeekGamesList();

        Writer.WriteSInt32(games.Count);

        foreach (Game game in games)
        {
            WriteGame(game);
        }

        ZeroUnusedSpaceInCurrentPage();
        ClearUnusedPages();

        FixActiveGameIndex();
    }

    private void WriteGame(Game game)
    {
        new GameEncoder(Writer).EncodeGame(game);
    }

    private void ZeroUnusedSpaceInCurrentPage()
    {
        while (Writer.Position % 256 != 0)
        {
            Writer.WriteByte(0x00);
        }
    }

    private void ClearUnusedPages()
    {
        while (Writer.Position < Writer.Buffer?.Length)
        {
            Writer.WriteByte(0xff);
        }
    }

    private void FixActiveGameIndex()
    {
        // 0x02FB02 = Sound (01 = On; 00 = Off)
        // 0x02FB04 = Background Image (02 = Default GameShark Logo; 03 = Green GameShark Logo)
        // 0x02FB05 = Currently Selected Game (00 = No Game Selected; else index of game starting at 1)
        // 0x02FB07 = Background Scroll (01 = On; 00 = Off)
        // 0x02FB6C = Menu Scroll (01 = On; 00 = Off)

        // the pristine decoded ROM from the ar3.enc file just has a block of 0xff where the
        // settings are usually stored. I assume this just causes it to write the default settings
        // on first use so just ignore it if this unused ROM image.
        if (Reader.Seek(0x0002FB00).ReadUInt16() != 0xffff)
        {
            // just reset the selected game back to nothing selected in case the existing
            // selected index is no longer valid or the game has changed after the update.
            Writer.Seek(0x0002FB05);
            Writer.WriteByte(0x00);
        }
    }
}
