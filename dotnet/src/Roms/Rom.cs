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
    private static readonly Color UnknownColor = Color.FromArgb(160, 160, 160);

    /// <summary>
    /// Plain, unencrypted, unobfuscated bytes.
    /// If the input file is encrypted/scrambled, it must be
    /// decrypted/unscrambled immediately in the subclass constructor.
    /// </summary>
    protected byte[] Buffer => Scribe.GetBufferCopy();

    public readonly RomMetadata Metadata;

    protected readonly List<Game> Games = new();
    protected readonly BinaryScribe Scribe;

    // TODO(CheatoBaggins): Compute file checksums
    protected Rom(
        string filePath,
        BinaryScribe scribe,
        GameConsole console,
        RomFormat format
    )
    {
        Scribe = scribe;
        Metadata = new RomMetadata
        {
            FilePath = filePath,
            Console = console,
            Format = format,
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
        Console.WriteLine();
        Console.WriteLine("File properties:");
        PrintFilePropTable();
        Console.WriteLine();
        Console.WriteLine("Identifiers:");
        PrintIdentifiers();
        Console.WriteLine();
        PrintCustomHeader();
        Console.WriteLine();
        PrintGames();
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
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

        foreach (Game game in Games)
        {
            string cheats = game.Cheats.Count == 1 ? "cheat" : "cheats";
            string gameIsActive = game.IsGameActive ? " <!------------ CURRENTLY SELECTED GAME" : "";
            Console.WriteLine($"- {game.GameName.Value} ({game.Cheats.Count} {cheats}){gameIsActive}");
            foreach (Cheat cheat in game.Cheats)
            {
                string codes = cheat.Codes.Count == 1 ? "code" : "codes";
                string isActive = cheat.IsCheatActive ? " [ON]" : "";
                Console.WriteLine($"    - {cheat.CheatName.Value} ({cheat.Codes.Count} {codes}){isActive}");
                foreach (Code code in cheat.Codes)
                {
                    string codeStr = code.Bytes.ToCodeString(Metadata.Console);
                    Console.WriteLine($"        {codeStr}");
                }
            }
        }
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
        filePropTable.AddRow("Version", Metadata.DisplayVersion.OrUnknown());
        filePropTable.AddRow("Build date", Metadata.BuildDateIso.OrUnknown());
        filePropTable.AddRow("Known ROM version", Metadata.IsKnownVersion);
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
