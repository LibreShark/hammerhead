using System.Diagnostics.CodeAnalysis;

namespace LibreShark.Hammerhead.N64;

/// <summary>
/// Base class for readers and writers of Game Shark ROM files.
/// </summary>
abstract class RomBase
{
    protected BinaryReader Reader { get; private set; }
    protected BinaryWriter Writer { get; private set; }

    protected RomBase() : this(new BinaryReader())
    {
    }

    protected RomBase(BinaryReader reader)
    {
        Reader = reader;
        Writer = new BinaryWriter(reader.Buffer ?? Array.Empty<byte>());
    }

    protected void ReadRomFromFile(string path)
    {
        BinaryReader rom = BinaryReader.FromFile(path);
        Reader = rom;

        if (!ValidateRom(rom))
        {
            throw new Exception("Not a valid N64 GameShark ROM");
        }

        Writer = new BinaryWriter(rom.Buffer ?? Array.Empty<byte>());
    }

    protected void ReadRomFromBytes(byte[] bytes)
    {
        BinaryReader rom = BinaryReader.FromBytes(bytes);
        Reader = rom;

        if (!ValidateRom(rom))
        {
            throw new Exception("Not a valid N64 GameShark ROM");
        }

        Writer = new BinaryWriter(rom.Buffer ?? Array.Empty<byte>());
    }

    protected RomVersion? ReadVersion()
    {
        SeekBuildTimestamp();
        return RomVersion.From(Reader.ReadPrintableCString(15));
    }

    protected List<KeyCode> ReadKeyCodes()
    {
        SeekActiveKeyCode();
        byte[] activePrefix = Reader.ReadBytes(8);

        SeekKeyCodeList();
        byte[] listBytes = Reader.PeekBytes(0xA0);
        int maxPos = Reader.Position + listBytes.Length;
        int keyCodeLength = IndexOf(listBytes, "Mario World 64 & Others");
        if (keyCodeLength < 9)
        {
            return new List<KeyCode>();
        }
        var keyCodes = new List<KeyCode>();
        while (Reader.Position <= maxPos)
        {
            byte[] bytes = Reader.ReadBytes(keyCodeLength);
            string name = Reader.ReadPrintableCString(0x1F);
            while (Reader.PeekBytes(1)[0] == 0)
            {
                Reader.ReadUByte();
            }
            var isActive = IndexOf(bytes, activePrefix) > -1;
            var keyCode = new KeyCode(name, bytes, isActive);
            keyCodes.Add(keyCode);
        }
        return keyCodes;
    }

    private static int IndexOf(byte[] haystack, string str)
    {
        byte[] needle = str.ToCharArray().Select((ch) => (byte)ch).ToArray();
        return IndexOf(haystack, needle);
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i < haystack.Length; i++)
        {
            for (int j = 0; j < needle.Length; j++)
            {
                if (needle[j] != haystack[i + j])
                {
                    break;
                }
                if (j == needle.Length - 1)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static bool Is(string a, string b)
    {
        return string.Compare(a, b, StringComparison.InvariantCulture) == 0;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private bool ValidateRom(BinaryReader rom)
    {
        int? bufferLength = rom.Buffer?.Length;
        if (bufferLength != 0x00040000)
        {
            // always 256 KiB
            Console.Error.WriteLine($"ERROR: Invalid GS ROM file size: 0x{bufferLength:X8}. All GameShark ROMs are exactly 256 KiB.");
            return false;
        }

        UInt32 gsRomMagicNumber = rom.Seek(0x00000000).ReadUInt32();
        if (gsRomMagicNumber != 0x80371240)
        {
            // N64 ROM Magic Number
            Console.Error.WriteLine($"ERROR: Invalid GS ROM magic number: 0x{gsRomMagicNumber:X8}. Expected 0x80371240.");
            return false;
        }

        string romHeader = rom.Seek(0x00000020).ReadPrintableCString(13);
        const string v1or2Header = "(C) DATEL D&D";
        const string v3ProHeader = "(C) MUSHROOM ";
        const string trainHeader = "Perfect Train";
        bool isV1or2 = Is(romHeader, v1or2Header);
        bool isV3Pro = Is(romHeader, v3ProHeader);
        bool isTrain = Is(romHeader, trainHeader);
        if (!isV1or2 && !isV3Pro && !isTrain)
        {
            // ROM Header Name
            Console.Error.WriteLine($"WARNING: Unknown GS ROM header name: '{romHeader}'. Expected '{v1or2Header}' or '{v3ProHeader}'.");
        }

        // Magic Number for user settings block. Only present in GS ROMs v3.0 and higher.
        if (ReadVersion()?.Number >= 2.5)
        {
            UInt16 userSettingsMagicNumber = rom.Seek(0x0002FB00).ReadUInt16();
            if (userSettingsMagicNumber != 0x4754 &&
                userSettingsMagicNumber != 0xffff)
            {
                Console.Error.WriteLine($"ERROR: Invalid magic number for user settings block: 0x{userSettingsMagicNumber:X4}. Expected 0x4754 or 0xffff.");
                return false;
            }
        }

        return true;
    }

    protected void WriteRomToFile(string path)
    {
        Writer.WriteToFile(path);
    }

    protected void SeekGamesList()
    {
        Seek(ReadVersion()?.Number >= 2.5 ? 0x00030000 : 0x0002E000);
    }

    protected void SeekStart()
    {
        Seek(0x00000000);
    }

    protected void SeekBuildTimestamp()
    {
        Seek(0x00000030);
    }

    protected void SeekActiveKeyCode()
    {
        Seek(0x00000010);
    }

    protected void SeekKeyCodeList()
    {
        Seek(ReadVersion()?.Number >= 2.50 ? 0x0002FC00 : 0x0002D800);
    }

    protected RomBase Seek(int address)
    {
        Reader.Seek(address);
        Writer.Seek(address);
        return this;
    }
}
