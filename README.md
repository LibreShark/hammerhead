# Hammerhead

A Swiss Army Knife for reading, writing, encrypting, and decrypting "video game enhancer" firmware dumps.

![Hammerhead icon](/assets/images/hammerhead-icon-256.png)

Written in cross-platform C# and .NET.

## Usage examples

### Dump ROM cheats to plain text

Dump the cheats from all supported input files to their default text file formats:

```bash
dotnet run --project dotnet/src/src.csproj -- \
    cheats dump *.{gb,gbc,gba}
```

![screenshot](/assets/screenshots/hammerhead-screenshot-20230705-cheats-dump-gbc.png)

### Copy cheats from one ROM to another

Copy decrypted cheats from a scrambled Xplorer 64 ROM file to an encrypted GameShark ROM file:

```bash
dotnet run --project dotnet/src/src.csproj -- \
    cheats copy \
        xplorer64-v1.000e-b1772-19990507-SCRAMBLED.bin \
        ar3-with-xplorer-cheats.enc \
        --overwrite
```

![screenshot](/assets/screenshots/hammerhead-screenshot-20230705-cheats-copy-gs-to-xp.png)

### Encrypt a ROM file

Encrypt a GameShark ROM file for use with Datel's official N64 Utils:

```bash
dotnet run --project dotnet/src/src.csproj -- \
    rom encrypt \
        gspro-3.30-20000404-custom-cheats.bin \
        ar3.enc \
        --overwrite
```

![screenshot](/assets/screenshots/hammerhead-screenshot-20230705-rom-encrypt-n64-gs.png)

### View parsed ROM data

View all information extracted by Hammerhead from a directory of ROM and cheat text files:

```bash
dotnet run --project dotnet/src/src.csproj -- \
    info ar3.enc
```

![screenshot](/assets/screenshots/hammerhead-screenshot-20230705-info-n64-gs.png)

### Shark MX to JSON

Dump GBC Shark MX contacts/messages to JSON:

```bash
dotnet run --project dotnet/src/src.csproj -- \
    info sharkmx.gbc \
        --print-format=json
```

![screenshot](/assets/screenshots/hammerhead-screenshot-20230705-info-n64-gs.png)

## Supported file formats

Full support:

| File format                | Detect | Read | Write |
|:-------------------------- |:------ |:---- |:----- |
| N64 GameShark Datel cheats | Yes    | Yes  | Yes   |
| N64 GameShark ROMs         | Yes    | Yes  | Yes   |
| N64 Xplorer 64 FCD cheats  | Yes    | Yes  | Yes   |
| N64 Xplorer 64 ROMs        | Yes    | Yes  | Yes   |
| Hammerhead JSON cheats     | Yes    | Yes  | Yes   |
| N64 EverDrive-64 X7 cheats | Yes    | Yes  | Yes   |

In progress:

| File format                           | Detect | Read | Write |
|:------------------------------------- |:------ |:---- |:----- |
| GB GameShark v2.x ROMs                | Yes    | Yes  |       |
| GBC Code Breaker ROMs                 | Yes    | Yes  |       |
| GBC GameShark v3.x ROMs               | Yes    | Yes  |       |
| GBC GameShark v4.x ROMs               | Yes    | Yes  |       |
| GBC Shark MX ROMs                     | Yes    | Yes  |       |
| GBC Xploder ROMs                      | Yes    | Yes  |       |
| GBA Datel GameShark ROMs              | Yes    | Yes  |       |
| GBA FCD GameShark & Code Breaker ROMs | Yes    | Yes  |       |

Planned:

| File format                        | Detect | Read | Write |
|:---------------------------------- |:------ |:---- |:----- |
| GBC GameShark v3.x cheats (\*.gcf) |        |      |       |
| OpenEmu XML cheats                 |        |      |       |
| N64 Project 64 v1.x cheats         |        |      |       |
| N64 Project 64 v3.x cheats         |        |      |       |

Limited support:

| File format                       | Detect | Read | Write |
|:--------------------------------- |:------ |:---- |:----- |
| GBC BrainBoy / Monster Brain ROMs | Yes    |      |       |
| GBA TV Tuner ROMs                 | Yes    |      |       |
| N64 GB Hunter ROMs                | Yes    |      |       |

## Usage

1. [Install the .NET 7 SDK](https://learn.microsoft.com/en-us/dotnet/core/install/)

2. [Download N64 GameShark firmware images](https://github.com/LibreShark/sharkdumps)

3. Run the CLI with no arguments to see a list of supported commands:

    ```bash
    dotnet run --project dotnet/src/src.csproj
    ```

## Credits

This tool would not be possible without the _amazing_ reverse engineering work of:

- [@parasyte](https://github.com/parasyte)
- [@RWeick](https://github.com/RWeick/REF1329-N64-Gameshark-Clone)
- [@BacteriaMage](https://github.com/BacteriaMage/n64-gameshark-data-model)
