# Hammerhead

C# .NET utility for reading/writing cheat device firmware dumps (GameShark, Action Replay, Code Breaker, Xplorer64, etc.).

![Hammerhead icon](/assets/images/hammerhead-icon-256.png)

This project was forked from the fantastic [`BacteriaMage/n64-gameshark-data-model`](https://github.com/BacteriaMage/n64-gameshark-data-model) repo.

## Usage

1. [Install the .NET 7 SDK](https://learn.microsoft.com/en-us/dotnet/core/install/)

2. [Download N64 GameShark firmware images](https://github.com/LibreShark/sharkdumps)

3. Run the CLI with no arguments to see a list of supported commands:

    ```bash
    dotnet run --project dotnet/src/src.csproj
    ```

Example:

```bash
dotnet run --project dotnet/src/src.csproj -- \
    export-cheats ~/dev/libreshark/sharkdumps/n64/firmware/*.bin
```

You should see output like this:

```
--------------------------------------------

ar-1.11-19980415.bin

v1.11 (AR), built on 1998-04-15 14:56 ('14:56 Apr 15 98')

File checksums: {"Crc32":"8B19E7D6","Crc32C":"5FD06C56","MD5":"1BB21E903AF26C14743D0A7195B8EFA0","SHA1":"D8368950A490384E3B352F0E3FAD407C724B52EF"}

25 7E F2 25 7A 41 13 C0 BF - Mario World 64 & Others [ACTIVE]
A7 4E 5A 33 DC B4 DB EE FB - Diddy Kong Racing & 1080
BD 6E 59 15 94 28 CB FF EE - Yoshi Story

258 cheats for 26 games

--------------------------------------------

arpro-3.0-19990324.bin

v3.00 (AR), built on 1999-03-24 15:50 ('15:50 Mar 24 99')

File checksums: {"Crc32":"C992DFB4","Crc32C":"53476F7E","MD5":"35BA407EA9E4EF7C0ACE8B4F58BEEC41","SHA1":"D5E4E8C875D6BDA0AFAFB1B2513B16B1CB88DFC1"}

F1 29 7B C9 42 CD AE 9D 80 18 00 00 30 - Mario World 64 & Others [ACTIVE]
73 8B 63 5D 6C 38 3A 7C 80 20 10 00 A3 - Diddy, 1080, Banjo, Griffey
C2 FA 71 8A 60 EE B8 C4 80 20 04 00 18 - Yoshis, F-Zero, C'World
17 CB CD 32 43 9C 2D 88 80 19 00 00 84 - Zelda

506 cheats for 49 games

--------------------------------------------

arpro-3.3-20000418.bin

v3.30 (AR), built on 2000-04-18 16:08 ('16:08 Apr 18 ')

File checksums: {"Crc32":"9FBABFDA","Crc32C":"E5BDD114","MD5":"67AFA5DF80A5CFC91FCE1DC918EA0A4F","SHA1":"C3426114F5DA1FB7ABDE15A766D362698AD07166"}

A8 D1 DA F2 31 20 6B 53 80 18 00 00 44 - Mario World 64 & Others [ACTIVE]
9B D7 F2 70 03 01 02 09 80 20 10 00 78 - Diddy, 1080, Banjo, Griffey
A1 73 D6 30 7E 69 C7 EA 80 20 04 00 19 - Yoshis, F-Zero, C'World
96 74 63 AC DF AF D2 13 80 19 00 00 BE - Zelda

2,043 cheats for 181 games

--------------------------------------------

gs-1.02-19970801-dirty.bin

v0.00 (UNKNOWN), built on 1997-08-01 12:50 ('12:50 Aug 1 97')

File checksums: {"Crc32":"EB094CD4","Crc32C":"4AC9DB01","MD5":"A754FBD810C0CC4B7A6223C807A21312","SHA1":"22A6624EF7DB9EB12DEED5EF73D76B8DE8C2A2D7"}

No key codes found.

117 cheats for 20 games

--------------------------------------------

gs-1.04-19970819-corrupt-codes.bin

v1.04, built on 1997-08-19 10:35 ('10:35 Aug 19 97')

File checksums: {"Crc32":"73639BF2","Crc32C":"9D58D5EA","MD5":"208CC114A1B676849ED458C455FA10F7","SHA1":"753788BCC24DFA84BD91F8C14AAD1B3C4D44255C"}

No key codes found.

3 cheats for 2 games

--------------------------------------------

gs-1.04-19970819-valid-codes.bin

v1.04, built on 1997-08-19 10:35 ('10:35 Aug 19 97')

File checksums: {"Crc32":"5A44CA4B","Crc32C":"4BD30CE1","MD5":"3940CEEF1B6286C2A5BD79907485F167","SHA1":"654B0BA58FBCD800E8C5046F6E80ED97DC073ACA"}

No key codes found.

142 cheats for 22 games

--------------------------------------------

gs-1.05-19970904-dirty.bin

v1.05, built on 1997-09-04 16:25 ('16:25 Sep 4 97')

File checksums: {"Crc32":"1BB603A0","Crc32C":"F8B8DD2A","MD5":"2E04F7872539A1B7816F82C1877CDC01","SHA1":"185BB3B1BD8DD6B60D10FD9173026775002F7657"}

No key codes found.

133 cheats for 23 games

--------------------------------------------

gs-1.05-19970905-dirty.bin

v0.00 (UNKNOWN), built on 1997-09-05 13:51 ('13:51 Sep 5 97')

File checksums: {"Crc32":"4D2CD0BD","Crc32C":"BCC89F12","MD5":"1FD0CBAB78195291D5C99818032B3B34","SHA1":"C53DF5D0A4E1C2CC1D6C9810D534AD937CB95E6C"}

No key codes found.

146 cheats for 24 games

--------------------------------------------

gs-1.06-19970919-dirty.bin

v1.06, built on 1997-09-19 14:25 ('14:25 Sep 19 97')

File checksums: {"Crc32":"0D7FC585","Crc32C":"7ED83EE3","MD5":"2340995719DBF9A0B61FDD1C1B2DDC74","SHA1":"4D1619C19C95C9A7B4A7381E61F0B1FD0B02395E"}

No key codes found.

76 cheats for 21 games

--------------------------------------------

gs-1.07-19971107-dirty.bin

v1.07, built on 1997-11-07 10:24 ('10:24 Nov 7 97')

File checksums: {"Crc32":"FF4DBD51","Crc32C":"3C7265D0","MD5":"6C76A2B82F070DAD96D85B579B01016A","SHA1":"829DE52D0C228D487D981B265FAA2549B373ECF6"}

No key codes found.

169 cheats for 27 games

--------------------------------------------

gs-1.08-19971124-dirty.bin

v1.08 (November), built on 1997-11-24 11:58 ('11:58 Nov 24 97')

File checksums: {"Crc32":"269EACC5","Crc32C":"FF7D6B0E","MD5":"B7DF186D806FBD43E453FB859A4531B9","SHA1":"9BA2124498604D65AD46415916DEF4DBDA9C2B23"}

96 21 73 83 8C 8E 33 4F AA - Mario World 64 & Others [ACTIVE]
4E F8 4D D6 0A B3 D6 0A B8 - Diddy Kong Racing

69 cheats for 7 games

--------------------------------------------

gs-1.08-19971208-dirty.bin

v1.08 (December), built on 1997-12-08 11:10 ('11:10 Dec 8 97')

File checksums: {"Crc32":"8CCA7290","Crc32C":"B76813F3","MD5":"3EDAF696962F5EF785DF1290416E18CD","SHA1":"138E3A1D0C5BE4CBD8A3CDA1FE5BF61F95C8DDBF"}

AF 71 AE CD 45 A8 7F 75 F8 - Mario World 64 & Others [ACTIVE]
35 37 8C 4B 1C F7 BF BC BD - Diddy Kong Racing

109 cheats for 20 games

--------------------------------------------

gs-1.09-19980105-clean.bin

v1.09, built on 1998-01-05 17:40 ('17:40 Jan 5 98')

File checksums: {"Crc32":"707F44B5","Crc32C":"028C50E5","MD5":"750640E520900E8915A353F5E4ED0A0E","SHA1":"12324B369D78622D6EA3894F1BA730BF23BCCF77"}

40 16 18 06 4E CF CD 4A 05 - Mario World 64 & Others [ACTIVE]
59 A6 31 F5 13 B3 DA 50 FA - Diddy Kong Racing
05 63 14 98 D5 E4 CF CD 1A - Yoshi Story

165 cheats for 36 games

--------------------------------------------

gs-2.00-19980305-clean.bin

v2.00 (March), built on 1998-03-05 08:06 ('08:06 Mar 5 98')

File checksums: {"Crc32":"8647B235","Crc32C":"DF4E4314","MD5":"8E89279C5B76251D90B40233C25232B5","SHA1":"53FEF75A350DC21330CDA08E85EF624AAF66E72E"}

63 34 F1 61 A7 2C 20 1C 2E - Mario World 64 & Others [ACTIVE]
50 F2 49 08 7C 07 EE 6C 25 - Diddy Kong Racing
8D 9A 8C DA F5 F2 B6 07 92 - Yoshi Story

165 cheats for 36 games

--------------------------------------------

gs-2.00-19980406-clean.bin

v2.00 (April), built on 1998-04-06 10:05 ('10:05 Apr 6 98')

File checksums: {"Crc32":"EF9EDF87","Crc32C":"E0CE0456","MD5":"437EFD7FD7F84F4C0F802D3BF1F8464E","SHA1":"19148A009EF8E1013AB35C8141781184B141699F"}

16 FB 52 A4 7A ED 1F B3 17 - Mario World 64 & Others [ACTIVE]
93 AA 74 23 FF 7C 32 FB DE - Diddy Kong Racing
20 55 38 42 DC 8E E1 C7 C9 - Yoshi Story

165 cheats for 36 games

--------------------------------------------

gs-2.10-19980825-clean.bin

v2.10, built on 1998-08-25 13:57 ('13:57 Aug 25 98')

File checksums: {"Crc32":"6D32F9F1","Crc32C":"1822C3ED","MD5":"0D4872A5DA1FE4531AB17959CEDE0B3C","SHA1":"C0EEEE91F0C7650BA75935A3E7D809423E8EFA0F"}

EB 03 0C 2C D2 3A AF C3 CE - Mario World 64 & Others [ACTIVE]
78 69 4F BD AC EF E9 DD 79 - Diddy, 1080, Banjo, Griffey
85 A2 B3 44 44 4C F1 C1 E4 - Yoshis Story

336 cheats for 61 games

--------------------------------------------

gs-2.21-19981218-clean.bin

v2.21, built on 1998-12-18 12:47 ('12:47 Dec 18 98')

File checksums: {"Crc32":"4A3EB21F","Crc32C":"654D394A","MD5":"0E48FAFE65A7580F92299ECF985454C9","SHA1":"FB0A7105E9CA548BDE8AD8B175EB1B24822A5649"}

1E B8 7C F0 86 12 C2 A2 80 20 10 00 63 - Mario World 64 & Others [ACTIVE]
46 01 56 4E 26 01 D2 BC 80 20 10 00 DB - Diddy, 1080, Banjo, Griffey
EA 25 09 0A 40 69 FB C9 80 20 10 00 A4 - Yoshis, F-Zero, C'World
79 5E 19 BA 53 7F 71 DA 80 19 00 00 65 - Zelda

618 cheats for 106 games

--------------------------------------------

gs-2.50-xxxx0504-v3.3-codes.bin

v2.50, built on 1999-05-04 12:58 ('12:58 May 4 ')

File checksums: {"Crc32":"710FC1E3","Crc32C":"E6D1D271","MD5":"038E0C19F6600AD48DF746F663470BFA","SHA1":"AEB4D73C03CEE32926FFB1B35D6C7D5646C0FE93"}

CD 78 7C FD 55 BB BF 05 80 18 00 00 BA - Mario World 64 & Others [ACTIVE]
9A 3B D6 6C 37 2E 4C DA 80 20 10 00 FE - Diddy, 1080, Banjo, Griffey
F7 26 1E CC 3A 9D 64 0C 80 20 04 00 90 - Yoshis, F-Zero, C'World
E6 95 89 4B 80 86 C1 F7 80 19 00 00 FA - Zelda

2,093 cheats for 188 games

--------------------------------------------

gspro-3.00-19990401-clean.bin

v3.00, built on 1999-04-01 15:05 ('15:05 Apr 1 99')

File checksums: {"Crc32":"46B188EC","Crc32C":"6947B7D9","MD5":"40883CCF08FE37D16F002133D989840B","SHA1":"54AE94B2BBCDAA7FFDB4B0AD1B283A9936341592"}

70 14 FF AB 1A 91 14 49 80 18 00 00 B4 - Mario World 64 & Others [ACTIVE]
5B E5 5F CE 93 89 D7 11 80 20 10 00 9F - Diddy, 1080, Banjo, Griffey
33 31 66 BD 04 ED E3 62 80 20 04 00 DF - Yoshis, F-Zero, C'World
56 72 19 E1 9D 62 82 28 80 19 00 00 C9 - Zelda

1,124 cheats for 120 games

--------------------------------------------

gspro-3.10-19990609-clean.bin

v3.10, built on 1999-06-09 16:50 ('16:50 Jun 9 99')

File checksums: {"Crc32":"3A364D6D","Crc32C":"E428D7F5","MD5":"717AC1AEB626CFE878E56529DE189DE8","SHA1":"08611A33D17CAD942ED5FD5DB5E9C2BF24223910"}

A9 25 39 DA 0C D8 E5 48 80 18 00 00 EC - Mario World 64 & Others [ACTIVE]
1F 94 99 78 94 F6 B7 55 80 20 10 00 97 - Diddy, 1080, Banjo, Griffey
07 78 28 4A A7 CA 56 C3 80 20 04 00 D7 - Yoshis, F-Zero, C'World
53 C8 DF 37 69 74 59 DB 80 19 00 00 DC - Zelda

1,124 cheats for 120 games

--------------------------------------------

gspro-3.20-19990622-clean.bin

v3.20, built on 1999-06-22 18:45 ('18:45 Jun 22 99')

File checksums: {"Crc32":"1C29EEFE","Crc32C":"4C1905D3","MD5":"D18D6A5ACF601007EC45076006DE58AB","SHA1":"775F2FC56BD037D2AB9CAB18C897AF02048C45CD"}

AF FA 90 67 C2 49 22 D0 80 18 00 00 12 - Mario World 64 & Others [ACTIVE]
BD B8 AF 1A E9 C2 8B 3B 80 20 10 00 30 - Diddy, 1080, Banjo, Griffey
B6 F4 6A E1 8B 0F C8 AB 80 20 04 00 67 - Yoshis, F-Zero, C'World
85 87 29 C5 3A 85 F7 50 80 19 00 00 F0 - Zelda

1,143 cheats for 122 games

--------------------------------------------

gspro-3.21-20000104-pristine.bin

v3.21, built on 2000-01-04 14:26 ('14:26 Jan 4 ')

File checksums: {"Crc32":"BD822452","Crc32C":"82A0D28C","MD5":"A588BD9DC516178BCF55B369A8CFEEB8","SHA1":"40C60B4499268F00B5202F0A40382EFD8D3C38A7"}

F5 0A 8A 93 42 3E 44 F5 80 18 00 00 3D - Mario World 64 & Others [ACTIVE]
E3 08 64 EA F5 15 E0 4A 80 20 10 00 E9 - Diddy, 1080, Banjo, Griffey
8F B7 7D 16 09 E6 49 7E 80 20 04 00 49 - Yoshis, F-Zero, C'World
DF 56 F3 35 C6 8A 9B A9 80 19 00 00 93 - Zelda

1,143 cheats for 122 games

--------------------------------------------

gspro-3.30-20000327-pristine.bin

v3.30 (March), built on 2000-03-27 09:54 ('09:54 Mar 27 ')

File checksums: {"Crc32":"C616A684","Crc32C":"E87C0FB3","MD5":"EA635EBC4F58481BE60D6D532C232870","SHA1":"D7B7DDF211C93BF4D5FCA959F7E850FD5CDA373D"}

8F 89 AB A0 C3 4C 26 10 80 18 00 00 A4 - Mario World 64 & Others [ACTIVE]
95 AC 21 BE 58 B0 4E F6 80 20 10 00 A8 - Diddy, 1080, Banjo, Griffey
C4 6F 1B C2 6C 6C 1F 67 80 20 04 00 1D - Yoshis, F-Zero, C'World
A9 24 53 52 5F 73 77 37 80 19 00 00 7D - Zelda

2,093 cheats for 188 games

--------------------------------------------

gspro-3.30-20000404-pristine.bin

v3.30 (April), built on 2000-04-04 15:56 ('15:56 Apr 4 ')

File checksums: {"Crc32":"1767FAFD","Crc32C":"EF6DDD55","MD5":"BF18FEBB24E7B860E3A63FF852078F63","SHA1":"5AC1387C18B0632F7D4C1106EB591D801835C57E"}

EA 6D 5B F8 E2 B4 69 6C 80 18 00 00 2B - Mario World 64 & Others [ACTIVE]
C3 5B B1 82 D0 4C A8 E9 80 20 10 00 32 - Diddy, 1080, Banjo, Griffey
96 EA 31 6E 54 70 CB AF 80 20 04 00 E4 - Yoshis, F-Zero, C'World
F0 03 23 12 77 F8 87 1C 80 19 00 00 F5 - Zelda

2,093 cheats for 188 games

--------------------------------------------

perfect_trainer-1.0b-20030618.bin

v1.00 (Perfect Trainer 1.0b), built on 2003-06-18 00:00 ('2003 iCEMARi0  ')

File checksums: {"Crc32":"D24C0646","Crc32C":"4F8A166F","MD5":"F40E92FE5C84E0F4E418E2750367F5E2","SHA1":"7F4C90932D6EE2957026CACEFF31527C188AC12E"}

No key codes found.

0 cheats for 0 games
```
