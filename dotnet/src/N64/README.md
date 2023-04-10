# N64 GameShark C# Data Model

Just some quick-and-dirty classes representing the Games/Cheats/Codes data model used by the [Nintendo 64 GameShark](https://gameshark.fandom.com/wiki/Nintendo_64) v1.08 and newer, plus a couple of helper classes for reading and writing this data. The helpers can read and write the binary formats the GameShark itself uses as well the text format used by the [official N64 Utility program](http://web.archive.org/web/20110426190730/http://gscentral.org/tools/n64/gs_pro_utils.zip). I created these classes while playing around with some of the devices in my personal collection.

## Note Files

The GameShark Pro can read and write the cheat list for a single game to and from a note file on the controller pak (press the Z button from the games list). The [NoteWriter](https://github.com/BacteriaMage/n64-gameshark-data-model/blob/main/Binary/NoteWriter.cs) and [NoteReader](https://github.com/BacteriaMage/n64-gameshark-data-model/blob/main/Binary/NoteReader.cs) classes can read and write this data to and from a ".note" file used with the [MPKEdit](https://bryc.github.io/mempak/) memory card editor.

## List Files

The [official N64 Utilities program](http://web.archive.org/web/20110426190730/http://gscentral.org/tools/n64/gs_pro_utils.zip) from Interact allows communication with a GameShark Pro 3.1 and 3.2 using the parallel port on a compatible PC. This utility can import and export the full game and cheat list directly to and from the GameShark cartridge using a specially formatted text file. Text files in this format can be generated and parsed by the [ListWriter](https://github.com/BacteriaMage/n64-gameshark-data-model/blob/main/Text/ListWriter.cs) and [ListReader](https://github.com/BacteriaMage/n64-gameshark-data-model/blob/main/Text/ListReader.cs) classes, respectively.

## ROM Files

The GameShark stores the game and cheat lists in the last quarter of the same flash memory chip where the program "ROM" is stored (not actually a ROM; just colloquially referring to the firmware). This means any ROM image for the GameShark contains a complete list of games and codes which can be read and updated. The [RomReader](https://github.com/BacteriaMage/n64-gameshark-data-model/blob/main/Binary/RomReader.cs) and [RomWriter](https://github.com/BacteriaMage/n64-gameshark-data-model/blob/main/Binary/RomWriter.cs) classes can read and write this binary data in the format that it is stored in on the device.

## ROM Encryption

The N64 Utilities program has an update feature which writes a fresh ROM to the device updating it to version 3.3 and optionally resets the games list. The accompanying "ar3.enc" file contains the complete 256 KB image including the updated GameShark program and default games list but the file is encrypted so that it cannot be directly read or modified by users. The [RomCrypter](https://github.com/BacteriaMage/n64-gameshark-data-model/blob/main/Binary/RomCrypter.cs) class can encode or decode such a file allowing the official image to be examined, or modified, or replaced outright with a different version. This code is based on an [earlier implementation in C by Hanimar](http://web.archive.org/web/20160324145321/http://doc.kodewerx.org/tools/n64/gs_n64_crypt.zip).
