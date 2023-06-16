namespace LibreShark.Hammerhead.N64;

using u32 = UInt32;

public class RomInfo
{
    public readonly N64GsVersion Version;
    public readonly List<Game> Games;
    public readonly List<KeyCode> KeyCodes;
    public readonly Checksum? Checksum;
    public readonly KeyCode? ActiveKeyCode;

    public u32 HeaderOffset => 0x00000000;
    public u32 ActiveKeyCodeOffset => 0x00000010;
    public u32 BuildTimestampOffset => 0x00000030;
    public u32 KeyCodeListOffset => (u32)(Version.Number >= 2.50 ? 0x0002FC00 : 0x0002D800);
    public u32 GameListOffset => (u32)(Version.Number >= 2.50 ? 0x00030000 : 0x0002E000);

    public RomInfo(N64GsVersion version, List<Game> games, List<KeyCode> keyCodes, Checksum? checksum = null, KeyCode? activeKeyCode = null)
    {
        Version = version;
        Games = games;
        KeyCodes = keyCodes;
        Checksum = checksum;
        ActiveKeyCode = activeKeyCode;
    }
}
