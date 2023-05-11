namespace LibreShark.Hammerhead.N64;

public class RomInfo
{
    public readonly RomVersion Version;
    public readonly List<Game> Games;
    public readonly List<KeyCode> KeyCodes;
    public readonly Checksum? Checksum;

    public UInt32 StartOffset => 0x00000000;
    public UInt32 BuildTimestampOffset => 0x00000030;
    public UInt32 ActiveKeyCodeOffset => 0x00000010;
    public UInt32 KeyCodeListOffset => (UInt32)(Version.Number >= 2.50 ? 0x0002FC00 : 0x0002D800);
    public UInt32 GameListOffset => (UInt32)(Version.Number >= 2.50 ? 0x00030000 : 0x0002E000);

    public RomInfo(RomVersion version, List<Game> games, List<KeyCode> keyCodes, Checksum? checksum = null)
    {
        Version = version;
        Games = games;
        KeyCodes = keyCodes;
        Checksum = checksum;
    }
}
