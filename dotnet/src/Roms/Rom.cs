using System.Collections.Immutable;
using LibreShark.Hammerhead.N64;
using NeoSmart.PrettySize;

namespace LibreShark.Hammerhead.Roms;

public abstract class Rom
{
    /// <summary>
    /// Raw, unaltered bytes that were read in from the ROM file.
    /// May be encrypted or scrambled, depending on the device.
    /// </summary>
    protected readonly ImmutableArray<byte> InitialBytes;

    /// <summary>
    /// Plain, unencrypted, unobfuscated bytes.
    /// If the input file is encrypted/scrambled, it must be
    /// decrypted/unscrambled immediately in the subclass constructor.
    /// </summary>
    protected readonly byte[] Bytes;

    public readonly RomMetadata Metadata;

    protected List<Game> Games = new();

    protected Rom(string filePath, byte[] bytes, GameConsole console, RomFormat format)
    {
        InitialBytes = bytes.ToImmutableArray();
        Bytes = bytes.ToArray();
        Metadata = new RomMetadata
        {
            FilePath = filePath,
            Console = console,
            Format = format,
        };
    }

    protected abstract void PrintCustomHeader();

    public virtual bool FormatSupportsFileEncryption() { return false; }
    public virtual bool FormatSupportsFileScrambling() { return false; }
    public virtual bool FormatSupportsFirmwareCompression() { return false; }
    public virtual bool FormatSupportsUserPrefs() { return false; }

    public virtual bool IsFileEncrypted() { return false; }
    public virtual bool IsFileScrambled() { return false; }
    public virtual bool IsFirmwareCompressed() { return false; }
    public virtual bool HasUserPrefs() { return false; }

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
        Console.WriteLine($"{Metadata.Format.ToDisplayString()} file with length = 0x{Bytes.Length:X8} " +
                          $"({Bytes.Length} bytes = {PrettySize.Format(Bytes.Length)}): '{Metadata.FilePath}'");
        Console.WriteLine();
        Console.WriteLine($"Brand:               {Metadata.Brand.ToDisplayString()}");
        Console.WriteLine($"Console:             {Metadata.Console.ToDisplayString()}");
        Console.WriteLine($"Format:              {Metadata.Format.ToDisplayString()}");
        Console.WriteLine($"Locale:              {Metadata.LanguageIetfCode}");
        Console.WriteLine($"Version:             {Metadata.DisplayVersion}");
        Console.WriteLine($"Build date:          {Metadata.BuildDateIso}");
        Console.WriteLine($"Known ROM version:   {Metadata.IsKnownVersion}");
        if (FormatSupportsFileEncryption())
        {
            Console.WriteLine($"File encrypted:      {IsFileEncrypted()}");
        }
        if (FormatSupportsFileScrambling())
        {
            Console.WriteLine($"File scrambled:      {IsFileScrambled()}");
        }
        if (FormatSupportsFirmwareCompression())
        {
            Console.WriteLine($"Firmware compressed: {IsFirmwareCompressed()}");
        }
        if (FormatSupportsUserPrefs())
        {
            Console.WriteLine($"Pristine user prefs: {!HasUserPrefs()}");
        }
        Console.WriteLine();
        Console.WriteLine("Identifiers:");
        foreach (RomString id in Metadata.Identifiers)
        {
            Console.WriteLine($"{id.Addr.ToDisplayString()} = '{id.Value}'");
        }
        Console.WriteLine();
        PrintCustomHeader();
        Console.WriteLine();
        if (Games.Count > 0)
        {
            string games = Games.Count == 1 ? "game" : "games";
            Cheat[] allCheats = Games.SelectMany(game => game.Cheats).ToArray();
            Code[] allCodes = Games.SelectMany(game => game.Cheats).SelectMany(cheat => cheat.Codes).ToArray();

            string totalCheats = allCheats.Length == 1 ? "cheat" : "cheats";
            string totalCodes = allCodes.Length == 1 ? "code" : "codes";
            Console.WriteLine($"{Games.Count} {games}, {allCheats.Length:N0} {totalCheats}, {allCodes.Length:N0} {totalCodes}");
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
        else
        {
            Console.WriteLine("No games/cheats found");
        }
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
    }
}
