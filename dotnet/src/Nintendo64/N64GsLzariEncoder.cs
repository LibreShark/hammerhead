// ReSharper disable InconsistentNaming
#pragma warning disable CS0169

namespace LibreShark.Hammerhead.Nintendo64;

/// <summary>
/// <para>
/// N64 GameShark firmware v2.50 and above use this compression algorithm
/// firmware to store embedded files more compactly.
/// </para>
///
/// <para>
/// This is a C# port of
/// https://github.com/Jhynjhiruu/gameshark/blob/aeed3cb6478904f9479f56743238d0d0ecfbce78/src/lzari.c
/// which was decompiled to C from an N64 GameShark Pro v3.30 (April) ROM by
/// Jhynjhiruu.
/// </para>
///
/// <para>
/// The GameShark source code is in turn based on
/// https://github.com/e-n-f/lzss/blob/f370a64a7ce5e4c54cfe122ca441671c3faccc24/LZARI.C,
/// <c>1989-04-07</c> by Haruhiko Okumura.
/// </para>
///
/// <list type="table">
///   <item>
///     <term><c>shell.bin</c></term>
///     <description>Main UI</description>
///   </item>
///   <item>
///     <term><c>trainer.bin</c></term>
///     <description>Code Generator</description>
///   </item>
///   <item>
///     <term>
///       <c>lslogo4.pal</c> (US - LibreShark Pro v4.xx) or <br></br>
///       <c>gslogo2.pal</c> (US - GameShark v2.50) or <br></br>
///       <c>gslogo3.pal</c> (US - GameShark Pro v3.xx) or <br></br>
///       <c>arlogo3.pal</c> (EU - Action Replay Pro v3.xx) or <br></br>
///       <c>equal3.pal</c> (EU - Equalizer v3.xx) or <br></br>
///       <c>gblogo3.pal</c> (DE - Game Buster v3.xx)
///     </term>
///     <description>Startup logo PALETTE</description>
///   </item>
///   <item>
///     <term>
///       <c>lslogo4.bin</c> (US - LibreShark Pro v4.xx) or <br></br>
///       <c>gslogo2.bin</c> (US - GameShark v2.50) or <br></br>
///       <c>gslogo3.bin</c> (US - GameShark Pro v3.xx) or <br></br>
///       <c>arlogo3.bin</c> (EU - Action Replay Pro v3.xx) or <br></br>
///       <c>equal3.bin</c> (EU - Equalizer v3.xx) or <br></br>
///       <c>gblogo3.bin</c> (DE - Game Buster v3.xx)
///     </term>
///     <description>Startup logo IMAGE DATA</description>
///   </item>
///   <item>
///     <term><c>tile1.tg~</c></term>
///     <description>Startup background tile</description>
///   </item>
/// </list>
///
/// <para>See also:</para>
/// <list type="bullet">
///   <item>https://github.com/Jhynjhiruu/fsblob/blob/716e921e97b95ca075bf86b96589b89f7ef75b17/src/lib.rs</item>
///   <item>https://github.com/Jhynjhiruu/gameshark/blob/aeed3cb6478904f9479f56743238d0d0ecfbce78/tools/splat_ext/fsblob.py</item>
/// </list>
/// </summary>
public class N64GsLzariEncoder
{
    public u8[] Encode(u8[] inputFile) {
        int  i, c, len, r, s, last_match_length;

        text_buf = new u8[N + F - 1];
        lson = new int[N + 1];
        rson = new int[N + 257];
        dad = new int[N + 1];
        char_to_sym = new int[N_CHAR];
        sym_to_char = new int[N_CHAR + 1];
        sym_freq = new uint[N_CHAR + 1];
        sym_cum = new uint[N_CHAR + 1];
        position_cum = new uint[N + 1];
        textsize = 0;
        codesize = 0;
        buffer_putbit = 0;
        mask_putbit = 0x80;
        buffer = 0;
        mask = 0;
        low = 0;
        high = 0x20000;
        value = 0;
        outfile = new u8[inputFile.Length];
        infile = inputFile;
        uncompressed_len = inputFile.Length;
        textsize = (ulong)inputFile.Length;
        outfile_len = inputFile.Length;
        in_cursor = 0;
        out_cursor = 0;

        for(i = 0; i < 4; i++) {
            outfile[out_cursor++] = (u8)(uncompressed_len >> (i * 8));
        }
        codesize += 4;
        if (textsize == 0) {
            return new u8[0];
        }
        textsize = 0;
        StartModel();
        InitTree();
        s = 0;
        r = N - F;
        for (i = s; i < r; i++) {
            text_buf[i] = (u8)' ';
        }

        for(len = 0; len < F && in_cursor < uncompressed_len; len++) {
            c = infile[in_cursor++];
            text_buf[r + len] = (u8)c;
        }

        textsize = (ulong)len;
        for (i = 1; i <= F; i++) {
            InsertNode(r - i);
        }
        InsertNode(r);

        do {
            if (match_length > len) {
                match_length = len;
            }

            if (match_length > len) {
                match_length = len;
            }
            if (match_length <= THRESHOLD) {
                match_length = 1;
                EncodeChar(text_buf[r]);
            } else {
                EncodeChar(255 - THRESHOLD + match_length);
                EncodePosition(match_position - 1);
            }

            last_match_length = match_length;

            for(i = 0; (i < last_match_length) && (in_cursor < uncompressed_len); i++) {
                c = infile[in_cursor++];
                DeleteNode(s);

                text_buf[s] = (u8)c;

                if (s < F - 1) {
                    text_buf[s + N] = (u8)c;
                }

                s = (s + 1) & (N - 1);
                r = (r + 1) & (N - 1);

                InsertNode(r);
            }

            while (i++ < last_match_length) {
                DeleteNode(s);
                s = (s + 1) & (N - 1);
                r = (r + 1) & (N - 1);
                if (--len != 0) {
                    InsertNode(r);
                }
            }
        } while (len > 0);
        EncodeEnd();
        return outfile[..out_cursor];
    }

    public u8[] Decode(u8[] compressed)
    {
        int  i, j, k, r, c;
        ulong count;

        text_buf = new u8[N + F - 1];
        lson = new int[N + 1];
        rson = new int[N + 257];
        dad = new int[N + 1];
        char_to_sym = new int[N_CHAR];
        sym_to_char = new int[N_CHAR + 1];
        sym_freq = new uint[N_CHAR + 1];
        sym_cum = new uint[N_CHAR + 1];
        position_cum = new uint[N + 1];
        textsize = 0;
        codesize = 0;
        buffer_putbit = 0;
        mask_putbit = 0x80;
        buffer = 0;
        mask = 0;
        low = 0;
        high = 0x20000;
        value = 0;
        infile = compressed;
        in_cursor = 0;
        out_cursor = 0;

        textsize = 0;
        for (i = 0; i < 4; i++) {
            textsize |= (uint)(infile[in_cursor] << (i * 8));
            in_cursor++;
        }

        outfile_len = (int)textsize;
        outfile = new u8[outfile_len];
        if (textsize == 0) {
            return outfile;
        }
        StartDecode();
        StartModel();
        for (i = 0; i < 0xFC4; i++) {
            text_buf[i] = 0x20;
        }

        r = 0xFC4;
        count = 0;
        while (count < textsize) {
            c = DecodeChar();
            if (c < 0x100) {
                outfile[out_cursor++] = (u8)c;
                text_buf[r++] = (u8)c;
                r &= 0xFFF;
                count++;
            } else {
                i = (r - DecodePosition() - 1) & 0xFFF;
                j = c - 0xFD;
                for (k = 0; k < j; k++) {
                    c = text_buf[(i + k) & 0xFFF];
                    outfile[out_cursor++] = (u8)c;
                    text_buf[r++] = (u8)c;
                    r &= 0xFFF;
                    count++;
                }

            }
        }

        return outfile;
    }

    /// <summary>
    /// size of ring buffer
    /// </summary>
    private const u16 N = 4096;

    /// <summary>
    /// upper limit for match_length
    /// </summary>
    private const u8 F = 60;

    /// <summary>
    /// encode string into position and length if match_length is greater than this
    /// </summary>
    private const u8 THRESHOLD = 2;

    /// index for root of binary search trees
    private const u16 NIL = N;

    private const int M = 15;

    /// <summary>
    /// Q1 (= 2 to the M) must be sufficiently large, but not so large as the
    /// unsigned long 4 * Q1 * (Q1 - 1) overflows.
    /// </summary>
    private const u32 Q1 = 1 << M;
    private const u32 Q2 = 2 * Q1;
    private const u32 Q3 = 3 * Q1;
    private const u32 Q4 = 4 * Q1;
    private const u32 MAX_CUM = Q1 - 1;

    private const int N_CHAR = 256 - THRESHOLD + F;

    private const string D_80042A20 = "WRITE ERROR";

    private uint mask = 0;                     // 0x00000000
    private u32 mask_putbit = 0;               // 0x00000000
    private int[] sym_to_char = new int[0];    // 0x00000000
    private int[] char_to_sym = new int[0];    // 0x00000000
    private int in_cursor = 0;                 // 0x00000000
    private uint buffer = 0;                   // 0x00000000
    private u32 buffer_putbit;                 // 0x00000000
    private int match_length = 0;              // 0x00000000
    private uint[] sym_cum = new uint[0];      // 0x00000000
    private uint[] position_cum = new uint[0]; // 0x00000000
    private int[] dad = new int[0];            // 0x00000000
    private u8[] infile = new u8[0];           // 0x00000000
    private int out_cursor = 0;                // 0x00000000
    private int uncompressed_len = 0;          // 0x00000000
    private u8[] text_buf = new u8[0];         // 0x00000000
    private uint[] sym_freq = new uint[0];     // 0x00000000
    private int[] lson = new int[0];           // 0x00000000

    private u8[] outfile = new u8[0];          // 0x00000000
    private int[] rson = new int[0];           // 0x00000000
    private int outfile_len = 0;               // 0x00000000
    private int match_position = 0;            // 0x00000000

    private ulong textsize = 0;                // 0x00000000
    private ulong codesize = 0;                // 0x00000000
    private u32 low = 0;                       // 0x00000000
    private u32 high = 0;                      // 0x00020000
    private s32 value = 0;                     // 0x00000000
    private s32 shifts = 0;                    // 0x00000000

    private void Error(string msg)
    {
        Console.Error.WriteLine(msg);
    }

    private void PutBit(int bit) {
        if (bit != 0) {
            buffer_putbit |= mask_putbit;
        }
        mask_putbit >>= 1;
        if (mask_putbit == 0) {
            outfile[out_cursor++] = (u8)buffer_putbit;
            if (out_cursor >= outfile_len) {
                Error(D_80042A20);
                return;
            }
            buffer_putbit = 0;
            mask_putbit = 0x80;
            codesize++;
        }
    }

    private void FlushBitBuffer() {
        s32 i;
        for (i = 0; i < 7; i++) {
            PutBit(0);
        };
    }

    private int GetBit() {
        mask >>= 1;
        if (mask == 0) {
            if (in_cursor >= infile.Length)
            {
                return 0;
            }
            buffer = infile[in_cursor++];
            mask = 0x80;
        }
        return (buffer & mask) != 0 ? 1 : 0;
    }

    private void InitTree() {
        int i;

        for (i = (N + 1); i <= (N + 256); i++) {
            rson[i] = NIL;
        }

        for (i = 0; i < N; i++) {
            dad[i] = NIL;
        }
    }

    private void InsertNode(int r) {
        int  i, p, cmp, temp;
        u8[] key;

        cmp = 1;
        key = text_buf[r..];
        p = N + 1 + key[0];

        lson[r] = NIL;
        rson[r] = NIL;

        match_length = 0;
        for ( ; ; ) {
            if (cmp >= 0) {
                if (rson[p] != NIL) {
                    p = rson[p];
                } else {
                    rson[p] = r;
                    dad[r] = p;
                    return;
                }
            } else {
                if (lson[p] != NIL) {
                    p = lson[p];
                } else {
                    lson[p] = r;
                    dad[r] = p;
                    return;
                }
            }
            for (i = 1; i < F; i++) {
                cmp = key[i] - text_buf[p + i];
                if (cmp != 0) {
                    break;
                }
            }
            if (i > THRESHOLD) {
                if (i > match_length) {
                    match_position = (r - p) & (N - 1);
                    if ((match_length = i) >= F) break;
                } else if (i == match_length) {
                    temp = (r - p) & (N - 1);
                    if (temp < match_position) {
                        match_position = temp;
                    }
                }
            }
        }

        dad[r] = dad[p];
        lson[r] = lson[p];
        rson[r] = rson[p];

        dad[lson[p]] = r;
        dad[rson[p]] = r;

        if (rson[dad[p]] == p) {
            rson[dad[p]] = r;
        } else {
            lson[dad[p]] = r;
        }

        dad[p] = NIL;  /* remove p */
    }

    private void DeleteNode(int p) {
        s32 q;

        if (dad[p] == NIL) {
            return;
        }

        if (rson[p] == NIL) {
            q = lson[p];
        } else if (lson[p] == NIL) {
            q = rson[p];
        } else {
            q = lson[p];
            if (rson[lson[p]] != NIL) {
                do {
                    q = rson[q];
                } while (rson[q] != NIL);
                rson[dad[q]] = lson[q];
                dad[lson[q]] = dad[q];
                lson[q] = lson[p];
                dad[lson[p]] = q;
            }
            rson[q] = rson[p];
            dad[rson[p]] = q;
        }

        dad[q] = dad[p];

        if (rson[dad[p]] == p) {
            rson[dad[p]] = q;
        } else {
            lson[dad[p]] = q;
        }

        dad[p] = NIL;
    }

    private void StartModel() {
        int ch, sym, i;

        sym_cum[N_CHAR] = 0;
        for (sym = N_CHAR; sym >= 1; sym--) {
            ch = sym - 1;
            char_to_sym[ch] = sym;
            sym_to_char[sym] = ch;
            sym_freq[sym] = 1;
            sym_cum[sym-1] = sym_cum[sym] + 1;
        }
        sym_freq[0] = 0;
        position_cum[N] = 0;
        for  (i = N; i >= 1; i--) {
            position_cum[i - 1] = position_cum[i] + 10000 / ((u32)i + 200);
        }
    }

    private void UpdateModel(int sym) {
        s32 ch_i;
        s32 ch_sym;
        u32 c;
        s32 i;

        if (sym_cum[0] >= MAX_CUM) {
            c = 0;
            for (i = N_CHAR; i > 0; i--) {
                sym_cum[i] = c;
                c += (sym_freq[i] = (sym_freq[i] + 1) >> 1);
            }
            sym_cum[0] = c;
        }
        for (i = sym; sym_freq[i] == sym_freq[i - 1]; i--)
        {
        }

        if (i < sym) {
            ch_i = sym_to_char[i];
            ch_sym = sym_to_char[sym];

            sym_to_char[i] = ch_sym;
            sym_to_char[sym] = ch_i;

            char_to_sym[ch_i] = sym;
            char_to_sym[ch_sym] = i;
        }
        sym_freq[i]++;
        while (--i >= 0) {
            sym_cum[i]++;
        }
    }

    private void Output(int bit) {
        PutBit(bit);
        while (shifts > 0) {
            PutBit(bit == 0 ? 1 : 0);
            shifts--;
        };
    }

    private void EncodeChar(int ch) {
        int sym;
        ulong range;

        sym = char_to_sym[ch];
        range = high - low;
        high = low + (u32)(range * sym_cum[sym - 1] / sym_cum[0]);
        low +=       (u32)(range * sym_cum[sym    ] / sym_cum[0]);
        for ( ; ; ) {
            if (high <= Q2) {
                Output(0);
            } else if (low >= Q2) {
                Output(1);
                low -= Q2;
                high -= Q2;
            } else if ((low >= Q1) && (high <= Q3)) {
                shifts++;
                low -= Q1;
                high -= Q1;
            } else {
                break;
            }
            low += low;
            high += high;
        }
        UpdateModel(sym);
    }

    private void EncodePosition(int position) {
        ulong range;

        range = high - low;
        high = low + (u32)(range * position_cum[position    ]) / position_cum[0];
        low +=       (u32)(range * position_cum[position + 1]) / position_cum[0];
        for ( ; ; ) {
            if (high <= Q2) {
                Output(0);
            } else if (low >= Q2) {
                Output(1);
                low -= Q2;
                high -= Q2;
            } else if ((low >= Q1) && (high <= Q3)) {
                shifts++;
                low -= Q1;
                high -= Q1;
            } else {
                break;
            }
            low += low;
            high += high;
        }
    }

    private void EncodeEnd() {
        shifts++;
        if(low < Q1) {
            Output(0);
        } else {
            Output(1);
        }
        FlushBitBuffer();
    }

    private int BinarySearchSym(uint x) {
        int i = 1, j = N_CHAR, k;
        while (i < j) {
            k = (i + j) / 2;
            if (x < sym_cum[k]) {
                i = k + 1;
            } else {
                j = k;
            }
        }
        return i;
    }

    private int BinarySearchPos(uint x) {
        int i = 1, j = N, k;
        while (i < j) {
            k = (i + j) / 2;
            if (x < position_cum[k]) {
                i = k + 1;
            } else {
                j = k;
            }
        }
        return i - 1;
    }

    private void StartDecode() {
        int i;
        for (i = 0; i < (M + 2); i++) {
            value = 2 * value + GetBit();
        };
    }

    private int DecodeChar() {
        int sym, ch;
        ulong range;

        range = high - low;
        sym = BinarySearchSym((u32)(((value - low + 1) * sym_cum[0] - 1) / (long)range));
        high = low + (u32)(range * sym_cum[sym - 1]) / sym_cum[0];
        low +=       (u32)(range * sym_cum[sym    ]) / sym_cum[0];
        for ( ; ; ) {
            if (low >= Q2) {
                value -= (s32)Q2;
                low -= Q2;
                high -= Q2;
            } else if ((low >= Q1) && (high <= Q3)) {
                value -= (s32)Q1;
                low -= Q1;
                high -= Q1;
            } else if (high > Q2) {
                break;
            }
            low += low;
            high += high;
            value = 2 * value + GetBit();
        }
        ch = sym_to_char[sym];
        UpdateModel(sym);
        return ch;
    }

    private int DecodePosition() {
        int position;
        ulong range;

        range = high - low;
        position = BinarySearchPos((u32)(((value - low + 1) * position_cum[0] - 1) / (long)range));
        high = low + (u32)(range * position_cum[position    ]) / position_cum[0];
        low +=       (u32)(range * position_cum[position + 1]) / position_cum[0];
        for ( ; ; ) {
            if (low >= Q2) {
                value -= (s32)Q2;
                low -= Q2;
                high -= Q2;
            } else if ((low >= Q1) && (high <= Q3)) {
                value -= (s32)Q1;
                low -= Q1;
                high -= Q1;
            } else if (high > Q2) {
                break;
            }
            low += low;
            high += high;
            value = 2 * value + GetBit();
        }
        return position;
    }
}
