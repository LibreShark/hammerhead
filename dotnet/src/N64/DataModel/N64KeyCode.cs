namespace LibreShark.Hammerhead.N64;

using u8 = Byte;
using u32 = UInt32;

public class N64KeyCode
{
    public readonly string Name;
    public readonly byte[] Bytes;
    public readonly bool IsActive;

    /// The first 8 bytes of a key code are the CRC32 checksums of the GS ROM's IPL3 chunk and firmware program chunk.
    public byte[] ChecksumBytes => Bytes.Take(8).ToArray();

    /// According to http://en64.shoutwiki.com/wiki/ROM#Cartridge_ROM_Header 0x08 is the Program Counter:
    ///
    /// > The program counter (PC) sets the boot location (RAM entry point) when preforming certain kinds of resets
    /// > (0x8000030C), however some CIC chips will alter the location like how they effect the CRC calculation.
    ///
    /// See also http://en64.shoutwiki.com/wiki/N64_Memory#Boot_Segment_and_Process
    public byte[] ProgramCounterBytes
    {
        get
        {
            if (Bytes.Length >= 12)
            {
                return Bytes.Skip(8).Take(4).ToArray();
            }
            return new byte[]{};
        }
    }

    /// CRC32 of the GS ROM's IPL3 chunk.
    public u32 Ipl3ChunkCrc32 => GetU32(0);

    /// CRC32 of the GS ROM's IPL3 chunk.
    public byte[] Ipl3ChunkCrcBytes => Bytes.Take(4).ToArray();

    // CRC32 of the GS ROM's firmware program chunk.
    public u32 ProgramChunkCrc32 => GetU32(4);

    // CRC32 of the GS ROM's firmware program chunk.
    public byte[] ProgramChunkCrcBytes => Bytes.Skip(4).Take(4).ToArray();

    public u8 CheckDigit => Bytes.Last();

    public N64KeyCode(string name, byte[] bytes, bool isActive)
    {
        Name = name;
        Bytes = bytes;
        IsActive = isActive;
    }

    public override string ToString()
    {
        string bytes = string.Join(" ", Bytes.Select((b) => $"{b:X2}"));
        string isActive = IsActive ? " [ACTIVE]" : "";
        return $"{bytes} - {Name}{isActive}";
    }

    private u32 GetU32(u32 addr = 0)
    {
        var b = Bytes.Skip((int)addr).Take(4).ToArray();
        var i = (b[0] << 24)
                | (b[1] << 16)
                | (b[2] << 8)
                | (b[3] << 0);
        return (u32)i;
    }
}
