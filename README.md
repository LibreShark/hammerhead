# Hammerhead

Swiss Army Knife for reading, writing, encrypting, and decrypting "video game enhancer" firmware dumps.

![Hammerhead icon](/assets/images/hammerhead-icon-256.png)

Written in cross-platform C# and .NET.

## Supported file formats

Full support:

| File format                | Detect | Read | Write |
|:-------------------------- |:------ |:---- |:----- |
| N64 GameShark Datel cheats | Yes    | Yes  | Yes   |
| N64 GameShark ROMs         | Yes    | Yes  | Yes   |
| N64 Xplorer 64 FCD cheats  | Yes    | Yes  | Yes   |
| N64 Xplorer 64 ROMs        | Yes    | Yes  | Yes   |
| Hammerhead JSON cheats     | Yes    | Yes  | Yes   |

In progress:

| File format                           | Detect | Read | Write |
|:------------------------------------- |:------ |:---- |:----- |
| GB GameShark v2.x ROMs                | Yes    | Yes  |       |
| GBC Code Breaker ROMs                 | Yes    | Yes  |       |
| GBC GameShark v3.x ROMs               | Yes    | Yes  |       |
| GBC GameShark v3.x cheats (*.gcf)     |        |      |       |
| GBC GameShark v4.x ROMs               | Yes    | Yes  |       |
| GBC Shark MX ROMs                     | Yes    | Yes  |       |
| GBC Xploder ROMs                      | Yes    | Yes  |       |
| GBA Datel GameShark ROMs              | Yes    | Yes  |       |
| GBA FCD GameShark & Code Breaker ROMs | Yes    | Yes  |       |
| OpenEmu XML cheats                    |        |      |       |
| N64 EverDrive-64 X7 cheats            |        |      |       |
| N64 Project 64 v1.x cheats            |        |      |       |
| N64 Project 64 v3.x cheats            |        |      |       |

Limited support:

| File format                           | Detect | Read | Write |
|:------------------------------------- |:------ |:---- |:----- |
| GBC BrainBoy / Monster Brain ROMs     | Yes    |      |       |
| GBA TV Tuner ROMs                     | Yes    |      |       |
| N64 GB Hunter ROMs                    | Yes    |      |       |

## Screenshots

![Screenshot of hammerhead rom-info CLI output](/assets/screenshots/hammerhead-screenshot-20230518.png)

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
