using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using BetterConsoles.Core;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using Google.Protobuf;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.N64;

namespace LibreShark.Hammerhead.Roms;

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
public sealed class N64GbHunterRom : Rom
{
    private const GameConsole ThisConsole = GameConsole.Nintendo64;
    private const RomFormat ThisRomFormat = RomFormat.N64Gbhunter;

    private readonly s32[] _rle01Addresses;

    public N64GbHunterRom(string filePath, u8[] rawInput)
        : base(filePath, rawInput, MakeScribe(rawInput), ThisConsole, ThisRomFormat)
    {
        _rle01Addresses = rawInput.FindAll("RLE01");

        // TODO(CheatoBaggins): Detect Game Booster
        // Metadata.Brand = RomBrand.GbHunter;
    }

    public override bool FormatSupportsCustomCheatCodes()
    {
        return false;
    }

    private static BinaryScribe MakeScribe(u8[] rawInput)
    {
        u8[] output = rawInput.ToArray();
        return new BigEndianScribe(output);
    }

    public static bool Is(u8[] bytes)
    {
        bool is256KiB = bytes.IsKiB(256);
        return is256KiB && bytes.FindAll("RLE01").Length == 6;
    }

    public static bool Is(Rom rom)
    {
        return rom.Metadata.RomFormat == ThisRomFormat;
    }

    public static bool Is(RomFormat type)
    {
        return type == ThisRomFormat;
    }

    public override void PrintCustomHeader(TerminalPrinter printer, InfoCmdParams @params)
    {
        printer.PrintHeading("RLE01 addresses");
        PrintRLE01Addrs(printer);
    }

    // ReSharper disable once InconsistentNaming
    private void PrintRLE01Addrs(TerminalPrinter printer)
    {
        Table table = printer.BuildTable(builder =>
        {
            builder
                .AddColumn("Address", rowsFormat: printer.ValueCell())
                .AddColumn("Length", rowsFormat: printer.KeyCell());
        });

        foreach (s32 rle01Address in _rle01Addresses)
        {
            table.AddRow($"0x{rle01Address:X8}", "".OrUnknown());
        }

        Console.WriteLine(table);
    }
}
