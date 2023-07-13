using LibreShark.Hammerhead.Images;
using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Nintendo64;

public class N64GsImageEncoder
{
    private const int StartupLogoWidth = 320;
    private const int StartupLogoHeight = 224;
    private const int StartupLogoPosX = 24;
    private const int StartupLogoPosY = 40;

    #region Tile Graphics (`*.tg~` files)

    /// <summary>
    /// Decodes a Datel Tile Graphics (.tg~) image, in which each pixel
    /// is stored as a custom-encoded 16-bit RGBA5551 color.
    ///
    /// <para>
    /// This includes background tiles ("tile1.tg~"), the
    /// menu border sprite ("menuf.tg~"), and "bits.tg~".
    /// </para>
    ///
    /// <para>
    /// See
    /// https://n64squid.com/homebrew/n64-sdk/textures/image-formats/#16-bit
    /// </para>
    /// </summary>
    /// <param name="imageBytes">Decompressed <c>tile1.tg~</c> bytes</param>
    /// <param name="fileName">Name of the embedded file (e.g., <c>tile1.tg~</c>)</param>
    /// <param name="width">Force a specific image width</param>
    /// <param name="height">Force a specific image height</param>
    /// <returns>Decoded tile image</returns>
    public Image<Rgba32> DecodeTileGraphic(u8[] imageBytes, string fileName = "", int width = 0, int height = 0)
    {
        var pixels = new List<Rgba32>();
        for (int i = 0; i < imageBytes.Length;)
        {
            u8 b1 = imageBytes[i++];
            u8 b2 = imageBytes[i++];

            u16 encodedRgba5551 = (u16)((b1 << 8) | b2);

            // 16-bit RGBA colors are are stored in an encoded form in GameShark
            // ROMs for some reason.
            //
            // The series of transformations below is exactly what the real
            // GameShark firmware does when it reads images from the fsblob.

            // Swap the first 9 bits with the last 7 bits.
            u32 encodedFirst9Bits = (u32)(encodedRgba5551 & 0xFF80); // 0xFF80 = 1111 1111 1000 0000
            u32 encodedLast7Bits  = (u32)(encodedRgba5551 & 0x007F); // 0x007F = 0000 0000 0111 1111
            u32 decodedFirst7Bits = encodedLast7Bits << 9;
            u32 decodedLast9Bits  = encodedFirst9Bits >> 7;
            u32 decodedBitsUnshifted = decodedFirst7Bits | decodedLast9Bits;
            u32 decodedBitsShifted   = decodedBitsUnshifted >> 1;

            // Clear the MS-bit of every channel and disable transparency.
            //
            //          b[0] b[1] b[2] b[3]
            // 0x7BDE = 0111 1011 1101 1110
            //        = 01111 01111 01111 0
            //            R     G     B   A
            //
            // TODO(CheatoBaggins): This discards 4 bits of data, making
            // the operation impossible to reverse. However, it also means that
            // those 4 bits are not being used for image data, so the
            // *effective* resolution of each channel is only 4-bit, not 5-bit.
            u16 decodedRgba5551 = (u16)(decodedBitsShifted & 0x7BDE);

            u8 r5 = (u8)((decodedRgba5551 >> 11) & 0x1F); // 0x1F = 0001 1111
            u8 g5 = (u8)((decodedRgba5551 >>  6) & 0x1F);
            u8 b5 = (u8)((decodedRgba5551 >>  1) & 0x1F);
            u8 a1 = (u8)((decodedRgba5551 >>  0) & 0x01);
            u8 r8 = Rgb5ToRgb8(r5);
            u8 g8 = Rgb5ToRgb8(g5);
            u8 b8 = Rgb5ToRgb8(b5);
            bool isTransparent = a1 == 0;
            u8 a8 = (u8)(isTransparent ? 0xFF : 0);

            var pixel = new Rgba32(r8, g8, b8, a8);

            // TODO(CheatoBaggins): REVERSE THE CALCULATION
            // 0x8421 = 1000 0100 0010 0001 = ~0x7BDE
            // u16 reencodedReversed_1 = (u16)((decodedRgba5551 | 0x8421) << 1);
            // u16 reencodedRgba5551_1 = (u16)((reencodedReversed_1 << 7) | (reencodedReversed_1 >> 9));
            //
            // u16 reencodedReversed_2 = (u16)((decodedRgba5551) << 1);
            // u16 reencodedRgba5551_2 = (u16)((reencodedReversed_2 << 7) | (reencodedReversed_2 >> 9));
            //
            // if (reencodedReversed_1 == decodedBitsUnshifted)
            // {
            //     pixel = Rgba32.ParseHex("FFC0CB"); // Light pink
            // }
            // if (reencodedRgba5551_1 == encodedRgba5551)
            // {
            //     pixel = Rgba32.ParseHex("FF00FF"); // Bright pink
            // }
            // if (reencodedReversed_2 == decodedBitsUnshifted)
            // {
            //     pixel = Rgba32.ParseHex("228B22"); // Forest green
            // }
            // if (reencodedRgba5551_2 == encodedRgba5551)
            // {
            //     pixel = Rgba32.ParseHex("00FFFF"); // Cyan
            // }

            pixels.Add(pixel);
        }

        if (width == 0 || height == 0)
        {
            if (pixels.Count == 3072)
            {
                // Background tiles:
                // "tile1.tg~", "tile2.tg~", "tile3.tg~", "tile4.tg~"
                width = 64;
                height = 48;
            }
            else if (pixels.Count == 3120)
            {
                // UI menu border image sprite: "menuf.tg~"
                width = 48;
                height = 65;
            }
            else if (pixels.Count == 576)
            {
                // Unknown: "bits.tg~"
                width = 72;
                height = 8;
            }
        }

        var image = new Image<Rgba32>(width, height);
        int pixelPos = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = pixels[pixelPos++];
            }
        }
        return image;
    }


    public u8[] EncodeTileGraphic(Image<Rgba32> png)
    {
        int width = png.Width;
        int height = png.Height;

        // 16 bits (2 bytes) per pixel
        u8[] buffer = new u8[width * height * 2];
        var scribe = new BigEndianScribe(buffer);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Rgba32 pixel = png[x, y];
                bool isTransparent = pixel.A == 0xFF;
                u8 r5 = Rgb8ToRgb5(pixel.R);
                u8 g5 = Rgb8ToRgb5(pixel.G);
                u8 b5 = Rgb8ToRgb5(pixel.B);
                u8 a1 = (u8)(isTransparent ? 0 : 1);
                u16 decodedRgba5551 = (u16)(
                    (r5 << 11) |
                    (g5 <<  6) |
                    (b5 <<  1) |
                    (a1 <<  0)
                );

                // TODO(CheatoBaggins): This doesn't work for all pixels.
                u16 encodedReversed = (u16)((decodedRgba5551) << 1);
                u16 encodedRgba5551 = (u16)((encodedReversed << 7) | (encodedReversed >> 9));

                scribe.WriteU16(encodedRgba5551);
            }
        }

        return buffer;
    }

    #endregion

    #region Logo image

    /// <summary>
    /// Decodes the startup logo, which is stored as two separate files:
    /// a palette (.pal file) and image data (.bin file).
    /// This is a simple form of compression called "indexed color".
    /// </summary>
    /// <param name="paletteBytes">Decompressed <c>gslogo3.pal</c> bytes</param>
    /// <param name="dataBytes">Decompressed <c>gslogo3.bin</c> bytes</param>
    /// <param name="transparency">Optionally enable transparent backgrounds</param>
    /// <param name="transparentColor">Color that will be replaced by a transparent pixel</param>
    /// <returns>Decoded logo image</returns>
    public Image<Rgba32> DecodeStartupLogo(
        u8[] paletteBytes,
        u8[] dataBytes,
        bool transparency = true,
        Rgb24 transparentColor = new Rgb24() // black
    )
    {
        var indexedColors = new List<Rgba32>();
        for (int i = 0; i < paletteBytes.Length; )
        {
            u8 r = Rgb5ToRgb8(paletteBytes[i++]);
            u8 g = Rgb5ToRgb8(paletteBytes[i++]);
            u8 b = Rgb5ToRgb8(paletteBytes[i++]);
            var rgb24Color = new Rgb24(r, g, b);
            bool isTransparent = transparency && rgb24Color.Equals(transparentColor);
            var rgba32Color = new Rgba32((float)r/255, (float)g/255, (float)b/255, isTransparent ? 0 : 1);
            indexedColors.Add(rgba32Color);
        }

        Rgba32[] pixels = dataBytes.Select(colorIndex => indexedColors[colorIndex]).ToArray();
        var image = new Image<Rgba32>(StartupLogoWidth, StartupLogoHeight);
        int pixelPos = 0;
        for (int y = StartupLogoPosY + 1; y < StartupLogoHeight - StartupLogoPosY + 1; y++)
        {
            for (int x = StartupLogoPosX; x < StartupLogoWidth - StartupLogoPosX; x++)
            {
                image[x, y] = pixels[pixelPos++];
            }
        }
        return image;
    }

    public (u8[], u8[]) EncodeStartupLogo(
        Image<Rgba32> rgba,
        bool transparency = true,
        Rgb24 transparentColor = new Rgb24() // black
    )
    {
        var rgb = new Image<Rgb24>(StartupLogoWidth, StartupLogoHeight);
        for (int y = StartupLogoPosY + 1; y < StartupLogoHeight - StartupLogoPosY + 1; y++)
        {
            for (int x = StartupLogoPosX; x < StartupLogoWidth - StartupLogoPosX; x++)
            {
                Rgba32 pixel = rgba[x, y];
                bool isTransparent = pixel.A == 0;
                rgb[x, y] = transparency && isTransparent ? transparentColor : new Rgb24(pixel.R, pixel.G, pixel.B);
            }
        }
        return EncodeStartupLogo(rgb);
    }

    private class PixelComparator : IComparer<Rgb24>
    {
        public int Compare(Rgb24 x, Rgb24 y)
        {
            int bComparison = x.B.CompareTo(y.B);
            if (bComparison != 0) return bComparison;
            int gComparison = x.G.CompareTo(y.G);
            if (gComparison != 0) return gComparison;
            return x.R.CompareTo(y.R);
        }
    }

    public (u8[], u8[]) EncodeStartupLogo(Image<Rgb24> image)
    {
        if (image.Width != StartupLogoWidth || image.Height != StartupLogoHeight)
        {
            throw new ArgumentException(
                $"Startup logo must be exactly {StartupLogoWidth}x{StartupLogoHeight}, " +
                $"but got {image.Width}x{image.Height}."
            );
        }

        var imagePixels = new List<Rgb24>();
        var palettePixelSet = new SortedSet<Rgb24>(comparer: new PixelComparator());
        for (int y = StartupLogoPosY + 1; y < StartupLogoHeight - StartupLogoPosY + 1; y++)
        {
            for (int x = StartupLogoPosX; x < StartupLogoWidth - StartupLogoPosX; x++)
            {
                Rgb24 pixel = image[x, y];
                imagePixels.Add(pixel);
                palettePixelSet.Add(pixel);
            }
        }

        // Index values must fit inside a single u8
        if (palettePixelSet.Count > 255)
        {
            return EncodeStartupLogo(MedianCutColorQuantization.ReduceColorPalette(image, 255));
        }

        var palettePixelList = palettePixelSet.ToList();

        u8[] paletteBytes = palettePixelSet.SelectMany(PixelToRgb555).ToArray();
        u8[] dataBytes = imagePixels.Select(
            pixel => (u8)palettePixelList.IndexOf(pixel)).ToArray();

        return (paletteBytes, dataBytes);
    }

    private u8[] PixelToRgb555(Rgb24 pixel)
    {
        return new u8[]
        {
            Rgb8ToRgb5(pixel.R),
            Rgb8ToRgb5(pixel.G),
            Rgb8ToRgb5(pixel.B),
        };
    }

    #endregion

    #region Pixel conversion

    /// <summary>
    /// Converts a 5-bit RGB channel value to the equivalent 8-bit value.
    /// </summary>
    /// <param name="fiveBitChannel">5-bit color value (RGB5, 0-31)</param>
    /// <returns>Equivalent 8-bit color value (RGB8, 0-255)</returns>
    private static u8 Rgb5ToRgb8(u8 fiveBitChannel)
    {
        // See
        // https://n64squid.com/homebrew/n64-sdk/textures/image-formats/
        // https://developer.apple.com/documentation/accelerate/1642297-vimageconvert_rgba5551torgba8888
        double eightBitChannel = (double)fiveBitChannel * 255 / 31;
        return (u8)eightBitChannel;
    }

    /// <summary>
    /// Converts an 8-bit RGB channel value to the equivalent 5-bit value.
    /// </summary>
    /// <param name="eightBitChannel">8-bit color value (RGB8, 0-255)</param>
    /// <returns>Equivalent 5-bit color value (RGB5, 0-31)</returns>
    private static u8 Rgb8ToRgb5(u8 eightBitChannel)
    {
        // See
        // https://n64squid.com/homebrew/n64-sdk/textures/image-formats/
        // https://developer.apple.com/documentation/accelerate/1642297-vimageconvert_rgba5551torgba8888
        double fiveBitChannel = (double)eightBitChannel * 31 / 255;
        return (u8)Math.Ceiling(fiveBitChannel);
    }

    #endregion
}
