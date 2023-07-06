using System.Collections.Immutable;

namespace LibreShark.Hammerhead.Nintendo64;

// ReSharper disable BuiltInTypeReferenceStyle
using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using s64 = Int64;
using u64 = UInt64;
using f64 = Double;

/// <summary>
/// Encrypts/decrypts cheat codes used by <see cref="N64XpRom"/> files.
/// </summary>
public static class N64XpCrypter
{
    public static bool IsCodeEncrypted(IReadOnlyList<u8> code)
    {
        u8 opcodeByte = code[0];
        var opcodeEnum = (Xp64Opcode)opcodeByte;
        ImmutableArray<Xp64Opcode> knownOpcodes = Enum.GetValues<Xp64Opcode>().ToImmutableArray();
        bool isUnencrypted = knownOpcodes.Contains(opcodeEnum);
        return !isUnencrypted;
    }

    /// <summary>
    /// From https://doc.kodewerx.org/hacking_n64.html#xp_encryption.
    /// </summary>
    public static u8[] EncryptCodeMethod1(u8[] code)
    {
        u8 a0 = code[0];
        u8 a1 = code[1];
        u8 a2 = code[2];
        u8 a3 = code[3];
        u8 d0 = code[4];
        u8 d1 = code[5];
        a0 = (u8)((a0 ^ 0x68));
        a1 = (u8)((a1 ^ 0x81) - 0x2B);
        a2 = (u8)((a2 ^ 0x82) - 0x2B);
        a3 = (u8)((a3 ^ 0x83) - 0x2B);
        d0 = (u8)((d0 ^ 0x84) - 0x2B);
        d1 = (u8)((d1 ^ 0x85) - 0x2B);
        return new u8[] {a0, a1, a2, a3, d0, d1};
    }

    /// <summary>
    /// From https://doc.kodewerx.org/hacking_n64.html#xp_encryption.
    /// </summary>
    public static u8[] EncryptCodeMethod2(u8[] code)
    {
        u8 a0 = code[0];
        u8 a1 = code[1];
        u8 a2 = code[2];
        u8 a3 = code[3];
        u8 d0 = code[4];
        u8 d1 = code[5];
        a0 = (u8)((a0 ^ 0x68));
        a1 = (u8)((a1 ^ 0x01) - 0xAB);
        a2 = (u8)((a2 ^ 0x02) - 0xAB);
        a3 = (u8)((a3 ^ 0x03) - 0xAB);
        d0 = (u8)((d0 ^ 0x04) - 0xAB);
        d1 = (u8)((d1 ^ 0x05) - 0xAB);
        return new u8[] {a0, a1, a2, a3, d0, d1};
    }

    /// <summary>
    /// From https://doc.kodewerx.org/hacking_n64.html#xp_encryption.
    /// </summary>
    public static u8[] DecryptCodeMethod1(IReadOnlyList<u8> code)
    {
        u8 a0 = code[0];
        u8 a1 = code[1];
        u8 a2 = code[2];
        u8 a3 = code[3];
        u8 d0 = code[4];
        u8 d1 = code[5];
        a0 = (u8)((a0 ^ 0x68));
        a1 = (u8)((a1 + 0x2B) ^ 0x81);
        a2 = (u8)((a2 + 0x2B) ^ 0x82);
        a3 = (u8)((a3 + 0x2B) ^ 0x83);
        d0 = (u8)((d0 + 0x2B) ^ 0x84);
        d1 = (u8)((d1 + 0x2B) ^ 0x85);
        return new u8[] {a0, a1, a2, a3, d0, d1};
    }

    /// <summary>
    /// From https://doc.kodewerx.org/hacking_n64.html#xp_encryption.
    /// </summary>
    public static u8[] DecryptCodeMethod2(IReadOnlyList<u8> code)
    {
        u8 a0 = code[0];
        u8 a1 = code[1];
        u8 a2 = code[2];
        u8 a3 = code[3];
        u8 d0 = code[4];
        u8 d1 = code[5];
        a0 = (u8)((a0 ^ 0x68));
        a1 = (u8)((a1 + 0xAB) ^ 0x01);
        a2 = (u8)((a2 + 0xAB) ^ 0x02);
        a3 = (u8)((a3 + 0xAB) ^ 0x03);
        d0 = (u8)((d0 + 0xAB) ^ 0x04);
        d1 = (u8)((d1 + 0xAB) ^ 0x05);
        return new u8[] {a0, a1, a2, a3, d0, d1};
    }
}
