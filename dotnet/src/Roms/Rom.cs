using System.Collections.Immutable;
using System.Drawing;
using BetterConsoles.Colors.Extensions;
using BetterConsoles.Core;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using LibreShark.Hammerhead.IO;
using LibreShark.Hammerhead.N64;
using NeoSmart.PrettySize;

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

public abstract class Rom
{
    private static readonly Color TableHeaderColor = Color.FromArgb(152, 114, 159);
    private static readonly Color TableKeyColor = Color.FromArgb(160, 160, 160);
    private static readonly Color TableValueColor = Color.FromArgb(230, 230, 230);
    private static readonly Color SelectedColor = Color.FromArgb(0, 153, 0);
    private static readonly Color UnknownColor = Color.FromArgb(160, 160, 160);

    public readonly ImmutableArray<u8> RawInput;

    /// <summary>
    /// Plain, unencrypted, unobfuscated bytes.
    /// If the input file is encrypted/scrambled, it must be
    /// decrypted/unscrambled immediately in the subclass constructor.
    /// </summary>
    protected byte[] Buffer => Scribe.GetBufferCopy();

    public readonly RomMetadata Metadata;

    protected readonly List<Game> Games = new();
    protected readonly BinaryScribe Scribe;

    protected Rom(
        string filePath,
        IEnumerable<byte> rawInput,
        BinaryScribe scribe,
        GameConsole console,
        RomFormat format
    )
    {
        RawInput = rawInput.ToImmutableArray();
        Checksum checksum = Checksum.From(RawInput);
        Scribe = scribe;
        Metadata = new RomMetadata
        {
            FilePath = filePath,
            Console = console,
            Format = format,
            FileChecksum = new ChecksumResult()
            {
                Crc32Hex = checksum.Crc32Hex,
                Crc32CHex = checksum.Crc32CHex,
                Md5Hex = checksum.Md5Hex,
                Sha1Hex = checksum.Sha1Hex,
            },
        };
    }

    protected abstract void PrintCustomHeader();

    public virtual bool FormatSupportsFileEncryption()
    {
        return false;
    }

    public virtual bool FormatSupportsFileScrambling()
    {
        return false;
    }

    public virtual bool FormatSupportsFirmwareCompression()
    {
        return false;
    }

    public virtual bool FormatSupportsUserPrefs()
    {
        return false;
    }

    public virtual bool IsFileEncrypted()
    {
        return false;
    }

    public virtual bool IsFileScrambled()
    {
        return false;
    }

    public virtual bool IsFirmwareCompressed()
    {
        return false;
    }

    public virtual bool HasUserPrefs()
    {
        return false;
    }

    public static Rom FromFile(string romFilePath)
    {
        byte[] bytes = File.ReadAllBytes(romFilePath);

        if (N64GsRom.Is(bytes))
        {
            return new N64GsRom(romFilePath, bytes);
        }

        if (N64XpRom.Is(bytes))
        {
            return new N64XpRom(romFilePath, bytes);
        }

        if (GbcCbRom.Is(bytes))
        {
            return new GbcCbRom(romFilePath, bytes);
        }

        if (GbcXpRom.Is(bytes))
        {
            return new GbcXpRom(romFilePath, bytes);
        }

        if (GbcGsRom.Is(bytes))
        {
            return new GbcGsRom(romFilePath, bytes);
        }

        if (GbcSharkMxRom.Is(bytes))
        {
            return new GbcSharkMxRom(romFilePath, bytes);
        }

        if (GbaGsDatelRom.Is(bytes))
        {
            return new GbaGsDatelRom(romFilePath, bytes);
        }

        if (GbaGsFcdRom.Is(bytes))
        {
            return new GbaGsFcdRom(romFilePath, bytes);
        }

        if (GbaTvTunerRom.Is(bytes))
        {
            return new GbaTvTunerRom(romFilePath, bytes);
        }

        if (GbcMonsterBrainRom.Is(bytes))
        {
            return new GbcMonsterBrainRom(romFilePath, bytes);
        }

        return new UnknownRom(romFilePath, bytes);
    }

    public void PrintSummary()
    {
        PrintHeading("File properties");
        PrintFilePropTable();
        PrintHeading("File checksums");
        PrintChecksums();
        PrintHeading("Identifiers");
        PrintIdentifiers();
        Console.WriteLine();
        PrintCustomHeader();
        Console.WriteLine();
        PrintGames();
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
    }

    private void PrintChecksums()
    {
        var headerFormat = new CellFormat()
        {
            Alignment = Alignment.Left,
            FontStyle = FontStyleExt.Bold,
            ForegroundColor = TableHeaderColor,
        };

        Table filePropTable = new TableBuilder(headerFormat)
            .AddColumn("Algorithm",
                rowsFormat: new CellFormat(
                    foregroundColor: TableKeyColor,
                    alignment: Alignment.Left
                )
            )
            .AddColumn("Checksum",
                rowsFormat: new CellFormat(
                    foregroundColor: TableValueColor,
                    alignment: Alignment.Left,
                    innerFormatting: true
                )
            )
            .Build();

        filePropTable.AddRow("CRC-32 (standard)", Metadata.FileChecksum.Crc32Hex);
        filePropTable.AddRow("CRC-32C (Castagnoli)", Metadata.FileChecksum.Crc32CHex);
        filePropTable.AddRow("MD5", Metadata.FileChecksum.Md5Hex);
        filePropTable.AddRow("SHA-1", Metadata.FileChecksum.Sha1Hex);

        filePropTable.Config = TableConfig.Unicode();

        Console.WriteLine(filePropTable);
    }

    private static void PrintHeading(string heading)
    {
        Console.WriteLine();
        Console.WriteLine($"{heading}:");
    }

    private void PrintIdentifiers()
    {
        foreach (RomString id in Metadata.Identifiers)
        {
            Console.WriteLine($"{id.Addr.ToDisplayString()} = '{id.Value}'");
        }
    }

    private void PrintGames()
    {
        if (Games.Count == 0)
        {
            Console.WriteLine("No games/cheats found.".SetStyle(FontStyleExt.Bold));
            return;
        }

        Cheat[] allCheats = Games.SelectMany(game => game.Cheats).ToArray();
        Code[] allCodes = Games.SelectMany(game => game.Cheats).SelectMany(cheat => cheat.Codes).ToArray();
        string gamePlural = Games.Count == 1 ? "game" : "games";
        string cheatCountPlural = allCheats.Length == 1 ? "cheat" : "cheats";
        string codeCountPlural = allCodes.Length == 1 ? "code" : "codes";
        Console.WriteLine($"{Games.Count} {gamePlural}, " +
                          $"{allCheats.Length:N0} {cheatCountPlural}, " +
                          $"{allCodes.Length:N0} {codeCountPlural}:");
        Console.WriteLine();

        var headerFormat = new CellFormat()
        {
            Alignment = Alignment.Left,
            FontStyle = FontStyleExt.Bold,
            ForegroundColor = TableHeaderColor,
        };

        Table gameTable = new TableBuilder(headerFormat)
            .AddColumn("Name",
                rowsFormat: new CellFormat(
                    foregroundColor: TableKeyColor,
                    alignment: Alignment.Left,
                    innerFormatting: true
                )
            )
            .AddColumn("Cheats",
                rowsFormat: new CellFormat(
                    foregroundColor: TableValueColor,
                    alignment: Alignment.Right
                )
            )
            .AddColumn("Warnings",
                rowsFormat: new CellFormat(
                    foregroundColor: TableValueColor,
                    alignment: Alignment.Left
                )
            )
            .Build();

        gameTable.Config = TableConfig.Unicode();

        List<Game> sortedGames = Games.ToList();
        sortedGames.Sort((g1, g2) =>
            string.Compare(g1.GameName.Value, g2.GameName.Value, StringComparison.InvariantCulture));

        foreach (Game game in sortedGames)
        {
            string gameName = game.GameName.Value;
            if (game.IsGameActive)
            {
                gameName = $"{gameName}".ForegroundColor(SelectedColor).SetStyle(FontStyleExt.Bold | FontStyleExt.Underline);
            }
            gameTable.AddRow(gameName, game.Cheats.Count, game.Warnings.Count > 0 ? $"{game.Warnings.Count}" : "");
        }

        Console.WriteLine(gameTable);
    }

    private void PrintFilePropTable()
    {
        var headerFormat = new CellFormat()
        {
            Alignment = Alignment.Left,
            FontStyle = FontStyleExt.Bold,
            ForegroundColor = TableHeaderColor,
        };

        Table filePropTable = new TableBuilder(headerFormat)
            .AddColumn("Property",
                rowsFormat: new CellFormat(
                    foregroundColor: TableKeyColor,
                    alignment: Alignment.Left
                )
            )
            .AddColumn("Value",
                rowsFormat: new CellFormat(
                    foregroundColor: TableValueColor,
                    alignment: Alignment.Left,
                    innerFormatting: true
                )
            )
            .Build();

        string fileSize = $"{PrettySize.Format(Buffer.Length)} " +
                          $"(0x{Buffer.Length:X8} = {Buffer.Length} bytes)";

        filePropTable.AddRow("Binary format", Metadata.Format.ToDisplayString());
        filePropTable.AddRow("Platform", Metadata.Console.ToDisplayString());
        filePropTable.AddRow("Brand", Metadata.Brand.ToDisplayString());
        filePropTable.AddRow("Locale", Metadata.LanguageIetfCode.OrUnknown());
        filePropTable.AddRow("", "");
        filePropTable.AddRow("Version", Metadata.DisplayVersion.OrUnknown());
        filePropTable.AddRow("Build date", Metadata.BuildDateIso.OrUnknown());
        filePropTable.AddRow("Known ROM version", Metadata.IsKnownVersion);
        filePropTable.AddRow("", "");
        filePropTable.AddRow("File size", fileSize);

        if (FormatSupportsFileEncryption())
        {
            filePropTable.AddRow("File encrypted", IsFileEncrypted());
        }

        if (FormatSupportsFileScrambling())
        {
            filePropTable.AddRow("File scrambled", IsFileScrambled());
        }

        if (FormatSupportsFirmwareCompression())
        {
            filePropTable.AddRow("Firmware compressed", IsFirmwareCompressed());
        }

        if (FormatSupportsUserPrefs())
        {
            filePropTable.AddRow("Pristine user prefs", !HasUserPrefs());
        }

        filePropTable.Config = TableConfig.Unicode();

        Console.WriteLine(filePropTable);
    }
}
