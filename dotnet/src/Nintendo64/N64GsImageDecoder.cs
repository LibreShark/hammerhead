namespace LibreShark.Hammerhead.Nintendo64;

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

public class N64GsImageDecoder
{
    public Image<Rgba32> DecodeStartupTile(u8[] imageBytes)
    {
        var pixels = new List<Rgba32>();
        for (int i = 0; i < imageBytes.Length;)
        {
            u8 b1 = imageBytes[i++];
            u8 b2 = imageBytes[i++];

            u16 rawRgba5551 = (u16)((b1 << 8) | b2);

            u32 val1 = (u32)rawRgba5551 >> 7;
            u32 val2 = (u32)rawRgba5551 & 0x7F;
            u32 val3 = val2 << 9;
            u32 val4 = (val1 | val3) >> 1;
            u32 val5 = val4 & 0x7BDE;
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
        var image = new Image<Rgba32>(0x40, 0x30);
        int pixelPos = 0;
        for (int y = 0; y < 0x30; y++)
        {
            for (int x = 0; x < 0x40; x++)
            {
                // TODO(CheatoBaggins): Figure out why the number of pixels
                // does not match the expected dimensions of the image,
                // and remove this entire `if` statement.
                if (pixelPos >= pixels.Count)
                {
                    return image;
                }
                image[x, y] = pixels[pixelPos++];
            }
        }
        return image;
    }

    public Image<Rgba32> DecodeStartupLogo(
        u8[] paletteBytes,
        u8[] imageBytes,
        bool transparency = false,
        Rgb24 transparentColor = new Rgb24()
    )
    {
        // ReSharper disable InconsistentNaming
        const int POS_X = 24;
        const int POS_Y = 40;
        const int WIDTH = 320;
        const int HEIGHT = 224;
        // ReSharper enable InconsistentNaming

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
        var image = new Image<Rgba32>(WIDTH, HEIGHT);
        int pixelPos = 0;
        for (int y = POS_Y + 1; y < HEIGHT - POS_Y + 1; y++)
        {
            for (int x = POS_X; x < WIDTH - POS_X; x++)
            {
                image[x, y] = pixels[pixelPos++];
            }
        }
        return image;
    }

    /// <summary>
    /// Convert a 5-bit RGB channel value to 8-bit.
    /// </summary>
    /// <param name="rgb5Channel"></param>
    /// <returns></returns>
    private static u8 Rgb5ToRgb8(u8 rgb5Channel)
    {
        // See
        // https://n64squid.com/homebrew/n64-sdk/textures/image-formats/
        // https://developer.apple.com/documentation/accelerate/1642297-vimageconvert_rgba5551torgba8888
        double rgb8Channel = (double)rgb5Channel * 255 / 31;
        return (u8)rgb8Channel;
    }
}
