using System.Collections.Immutable;
using LibreShark.Hammerhead.N64;

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

    protected Rom(string filePath, byte[] bytes, RomFormat format)
    {
        InitialBytes = bytes.ToImmutableArray();
        Bytes = bytes.ToArray();
        Metadata = new RomMetadata
        {
            FilePath = filePath,
            Format = format,
        };
    }


    public void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"N64 GameShark ROM file: '{Metadata.FilePath}'");
        Console.WriteLine();
        Console.WriteLine($"Format:     {Metadata.Format.ToDisplayString()}");
        Console.WriteLine($"Brand:      {Metadata.Brand.ToDisplayString()}");
        Console.WriteLine($"Locale:     {Metadata.LanguageIetfCode}");
        Console.WriteLine($"Version:    {Metadata.DisplayVersion}");
        Console.WriteLine($"Build date: {Metadata.BuildDateIso}");
        Console.WriteLine($"Encrypted:  {IsEncrypted()}");
        Console.WriteLine($"Compressed: {IsCompressed()}");
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
            Console.WriteLine($"{Games.Count} {games}:");
            foreach (Game game in Games)
            {
                string cheats = game.Cheats.Count == 1 ? "cheat" : "cheats";
                Console.WriteLine($"- {game.Name} ({game.Cheats.Count} {cheats})");
            }
        }
        else
        {
            Console.WriteLine("No games/cheats found");
        }
    }

    protected abstract void PrintCustomHeader();

    public virtual bool SupportsEncryption() { return false; }
    public virtual bool SupportsScrambling() { return false; }
    public virtual bool SupportsCompression() { return false; }

    public virtual bool IsEncrypted() { return false; }
    public virtual bool IsScrambled() { return false; }
    public virtual bool IsCompressed() { return false; }

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
}
