using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Cli;
using LibreShark.Hammerhead.Codecs;
using LibreShark.Hammerhead.IO;
using Spectre.Console;

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
/// GB Hunter (aka Game Booster in the UK),
/// made by Datel.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class N64GbHunterRom : AbstractCodec
{
    private const ConsoleId ThisConsoleId = ConsoleId.Nintendo64;
    private const CodecId ThisCodecId = CodecId.N64GbhunterRom;

    public static readonly CodecFileFactory Factory = new(Is, Is, Create);

    public static N64GbHunterRom Create(string filePath, u8[] rawInput)
    {
        return new N64GbHunterRom(filePath, rawInput);
    }

    private readonly s32[] _rle01Addresses;

    public override CodecId DefaultCheatOutputCodec => CodecId.UnsupportedCodecId;

    public N64GbHunterRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsoleId, ThisCodecId)
    {
        Support.SupportsCheats = true;
        Support.SupportsFirmware = true;

        Support.HasCheats = true;
        Support.HasFirmware = true;

        _rle01Addresses = rawInput.FindAll("RLE01");

        // TODO(CheatoBaggins): Detect Game Booster
        // Metadata.BrandId = BrandId.GbHunter;
    }

    public override ICodec WriteChangesToBuffer()
    {
        throw new NotImplementedException();
    }

    private static AbstractBinaryScribe MakeScribe(u8[] rawInput)
    {
        u8[] output = rawInput.ToArray();
        return new BigEndianScribe(output);
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        return is256KiB && bytes.FindAll("RLE01").Length == 6;
    }

    public static bool Is(ICodec codec)
    {
        return codec.Metadata.CodecId == ThisCodecId;
    }

    public static bool Is(CodecId type)
    {
        return type == ThisCodecId;
    }

    public override void PrintCustomHeader(ICliPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading("RLE01 addresses");
        PrintRLE01Addrs(printer);
    }

    // ReSharper disable once InconsistentNaming
    private void PrintRLE01Addrs(ICliPrinter printer)
    {
        Table table = printer.BuildTable()
                .AddColumn(printer.HeaderCell("Address"))
                .AddColumn(printer.HeaderCell("Length"))
            ;

        foreach (s32 rle01Address in _rle01Addresses)
        {
            table.AddRow($"0x{rle01Address:X8}", "".OrUnknown());
        }

        printer.PrintTable(table);
    }
}
