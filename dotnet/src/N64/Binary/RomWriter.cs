// bacteriamage.wordpress.com

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

    public static void ToFileAndReset(ICollection<Game> games, string source, string target)
    {
        RomWriter writer = new RomWriter();

        writer.ReadRomFromFile(source);
        writer.WriteGames(games);
        writer.ResetActiveKeyCode();
        writer.ResetUserPreferences();
        writer.WriteRomToFile(target);
    }

    private RomWriter()
    {
    }

    private void WriteGames(ICollection<Game> games)
    {
        var version = ReadVersion();

        SeekGamesList();

        Writer.WriteSInt32(games.Count);

        var i = 0;
        foreach (Game game in games)
        {
            WriteGame(game, version, i);
            i++;
        }

        Writer.WriteByte(0x00);

        ClearUnusedSpaceInCurrentPage();
        ClearUnusedPages();
        ResetActiveGameIndex();
    }

    private void WriteGame(Game game, RomVersion? version, int gameIndex)
    {
        new GameEncoder(Writer, version).EncodeGame(game, gameIndex);
    }

    private void ClearUnusedSpaceInCurrentPage()
    {
        // Earlier GameSharks use 0x00 for padding, whereas later versions use 0xFF.
        byte lastByte = Reader.Seek(Reader.Length - 1).PeekBytes(1).First();
        while (Writer.Position % 256 != 0)
        {
            Writer.WriteByte(lastByte);
        }
    }

    private void ClearUnusedPages()
    {
        // Earlier GameSharks use 0x00 for padding, whereas later versions use 0xFF.
        byte lastByte = Reader.Seek(Reader.Length - 1).PeekBytes(1).First();
        // Earlier ROMs have non-zero bytes at 0x0003E000. I don't know what they're for,
        // so avoid overwriting them just to be safe.
        // TODO(CheatoBaggins): Figure out what's at 0x0003E000.
        while (Writer.Position < Writer.Buffer?.Length && Writer.Position < 0x0003E000)
        {
            Writer.WriteByte(lastByte);
        }
    }

    private void ResetActiveGameIndex()
    {
        // Earlier GameSharks don't store any user preferences.
        var isAtLeastV250 = ReadVersion()?.Number >= 2.5;
        if (!isAtLeastV250)
        {
            return;
        }

        // 0x02FB02 = Sound (01 = On; 00 = Off)
        // 0x02FB04 = Background Image (02 = Default GameShark Logo; 03 = Green GameShark Logo)
        // 0x02FB05 = Currently Selected Game (00 = No Game Selected; else index of game starting at 1)
        // 0x02FB07 = Background Scroll (01 = On; 00 = Off)
        // 0x02FB6C = Menu Scroll (01 = On; 00 = Off)

        // the pristine decoded ROM from the ar3.enc file just has a block of 0xff where the
        // settings are usually stored. I assume this just causes it to write the default settings
        // on first use so just ignore it if this unused ROM image.
        var isPristine = Reader.Seek(0x0002FB00).ReadUInt16() == 0xffff;
        if (isPristine)
        {
            return;
        }

        // just reset the selected game back to nothing selected in case the existing
        // selected index is no longer valid or the game has changed after the update.
        Writer.Seek(0x0002FB05);
        Writer.WriteByte(0x00);
    }

    private void ResetUserPreferences()
    {
        // Earlier GameSharks don't store any user preferences.
        var isAtLeastV250 = ReadVersion()?.Number >= 2.5;
        if (!isAtLeastV250)
        {
            return;
        }

        // just reset the selected game back to nothing selected in case the existing
        // selected index is no longer valid or the game has changed after the update.
        Writer.Seek(0x0002FB00);
        for (int i = 0; i < 0x70; i++)
        {
            Writer.WriteByte(0xFF);
        }
    }

    private void ResetActiveKeyCode()
    {
        var activeKeyCode = ReadActiveKeyCode();
        var keyCodes = ReadKeyCodes();
        if (keyCodes.Count == 0)
        {
            return;
        }

        var firstKeyCode = keyCodes.First();
        if (activeKeyCode.ChecksumBytes.SequenceEqual(firstKeyCode.ChecksumBytes))
        {
            return;
        }

        Writer.Seek(0x00000010);
        Writer.WriteBytes(firstKeyCode.ChecksumBytes);
        if (firstKeyCode.Bytes.Length >= 12)
        {
            Writer.Seek(0x00000008);
            // TODO(CheatoBaggins): Figure out why this differs from pristine ROM bytes.
            Writer.WriteBytes(firstKeyCode.ProgramCounterBytes);
        }
    }
}
