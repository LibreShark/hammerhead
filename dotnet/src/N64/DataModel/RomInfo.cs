namespace LibreShark.Hammerhead.N64;

public class RomInfo
{
    public readonly RomVersion Version;
    public readonly List<Game> Games;
    public readonly List<KeyCode> KeyCodes;
    public readonly Checksum? Checksum;

    public RomInfo(RomVersion version, List<Game> games, List<KeyCode> keyCodes, Checksum? checksum = null)
    {
        Version = version;
        Games = games;
        KeyCodes = keyCodes;
        Checksum = checksum;
    }
}
