namespace LibreShark.Hammerhead.Nintendo64;

public class N64GsImageDecoder
{
    /// <summary>
    /// Decodes a background tile image.
    /// </summary>
    /// <param name="imageBytes">Decompressed <c>tile1.tg~</c> bytes</param>
    /// <param name="fileName">Name of the embedded file (e.g., <c>tile1.tg~</c>)</param>
    /// <param name="width">Force a specific image width</param>
    /// <param name="height">Force a specific image height</param>
    /// <returns>Decoded tile image</returns>
    public Image<Rgba32> Decode16BitRgba(u8[] imageBytes, string fileName = "", int width = 0, int height = 0)
    {
        var pixels = new List<Rgba32>();
        for (int i = 0; i < imageBytes.Length;)
        {
            u8 b1 = imageBytes[i++];
            u8 b2 = imageBytes[i++];

            u16 rawRgba5551 = (u16)((b1 << 8) | b2);

            // 16-bit RGBA colors are are stored in an encoded form in GameShark
            // ROMs for some reason.
            // The series of transformations below is exactly what the real
            // GameShark firmware does when it reads images from the fsblob.
            u32 val1 = (u32)rawRgba5551 >> 7;
            u32 val2 = (u32)rawRgba5551 & 0x7F;
            u32 val3 = val2 << 9;
            u32 val4 = (val1 | val3) >> 1;
            u32 val5 = val4 & 0x7BDE;

            // 16-bit RGBA color. See
            // https://n64squid.com/homebrew/n64-sdk/textures/image-formats/#16-bit
            u16 realRgba5551 = (u16)val5;

            u8 r5 = (u8)((realRgba5551 >> 11) & 0x3E);
            u8 g5 = (u8)((realRgba5551 >>  6) & 0x3E);
            u8 b5 = (u8)((realRgba5551 >>  1) & 0x3E);
            u8 a1 = (u8)((realRgba5551 >>  0) & 0x01);
            u8 r8 = Rgb5ToRgb8(r5);
            u8 g8 = Rgb5ToRgb8(g5);
            u8 b8 = Rgb5ToRgb8(b5);
            bool isTransparent = a1 == 0;
            u8 a8 = (u8)(isTransparent ? 0xFF : 0);
            pixels.Add(new Rgba32(r8, g8, b8, a8));
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

    /// <summary>
    /// Decodes the startup logo, which is stored as two separate files:
    /// a palette (.pal file) and image data (.bin file).
    /// This is a simple form of compression called "indexed color".
    /// </summary>
    /// <param name="paletteBytes">Decompressed <c>gslogo3.pal</c> bytes</param>
    /// <param name="imageBytes">Decompressed <c>gslogo3.bin</c> bytes</param>
    /// <param name="transparency">Optionally enable transparent backgrounds</param>
    /// <param name="transparentColor">Color that will be replaced by a transparent pixel</param>
    /// <returns>Decoded logo image</returns>
    public Image<Rgba32> DecodeStartupLogo(
        u8[] paletteBytes,
        u8[] imageBytes,
        bool transparency = false,
        Rgb24 transparentColor = new Rgb24()
    )
    {
        const int posX = 24;
        const int posY = 40;
        const int width = 320;
        const int height = 224;

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

        Rgba32[] pixels = imageBytes.Select(colorIndex => indexedColors[colorIndex]).ToArray();
        var image = new Image<Rgba32>(width, height);
        int pixelPos = 0;
        for (int y = posY + 1; y < height - posY + 1; y++)
        {
            for (int x = posX; x < width - posX; x++)
            {
                image[x, y] = pixels[pixelPos++];
            }
        }
        return image;
    }

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
}
