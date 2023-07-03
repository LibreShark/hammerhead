using System.Text.RegularExpressions;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Codecs;

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
/// Monster Brain and Brain Boy for Game Boy Color and Game Boy Pocket,
/// made by Future Console Design (FCD) and Pelican Accessories.
/// </summary>
public sealed class GbcMonsterBrainRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.GameBoyColor;
    private const CodecId ThisCodecId = CodecId.GbcMonsterbrainRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static GbcMonsterBrainRom Create(string filePath, u8[] rawInput)
    {
        return new GbcMonsterBrainRom(filePath, rawInput);
    }

    private static readonly string[] KnownTitles =
    {
        "BrainBoy version 1.1",
        "Monster Brain v2.0 Platinum",
        "Monster Brain v3.6 Platinum",
    };

    public override CodecId DefaultCheatOutputCodec => CodecId.UnsupportedCodecId;

    private GbcMonsterBrainRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsFirmware = true;
        Support.HasFirmware = true;

        Metadata.BrandId = DetectBrand(rawInput);

        RomString id = Scribe.Seek(0).ReadPrintableCString(0x20).Trim();
        Metadata.Identifiers.Add(id);
        Metadata.IsKnownVersion = KnownTitles.Contains(id.Value);

        Match match = Regex.Match(id.Value, @"(?:v|version )(?<number>\d+\.\d+)(?<decorators>.*)");
        if (match.Success)
        {
            string numberStr = match.Groups["number"].Value.Trim();
            string decoratorStr = match.Groups["decorators"].Value.Trim();
            if (decoratorStr.Length > 1)
            {
                decoratorStr = " " + decoratorStr;
            }
            Metadata.DisplayVersion = $"v{numberStr}{decoratorStr}".Trim();
            Metadata.SortableVersion = Double.Parse(numberStr);
            if (decoratorStr.Length == 1)
            {
                char c = decoratorStr.ToLower()[0];
                int d = c - 0x60;
                // E.g., "v1.0c" -> "v1.03"
                Metadata.SortableVersion = Double.Parse($"{numberStr}{d}");
            }
        }
    }

    public override AbstractCodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        bool is512KiB = bytes.IsKiB(512);
        return (is256KiB || is512KiB) && Detect(bytes);
    }

    private static bool Detect(u8[] bytes)
    {
        return DetectBrand(bytes) != BrandId.UnknownBrand;
    }

    private static BrandId DetectBrand(u8[] bytes)
    {
        string id = bytes[..0x20].ToAsciiString();
        if (id.Contains("BrainBoy"))
        {
            return BrandId.Brainboy;
        }
        if (id.Contains("Monster Brain"))
        {
            return BrandId.MonsterBrain;
        }
        return BrandId.UnknownBrand;
    }

    public static bool Is(AbstractCodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }

    private static AbstractBinaryScribe MakeScribe(u8[] rawInput)
    {
        return new LittleEndianScribe(rawInput.ToArray());
    }
}
