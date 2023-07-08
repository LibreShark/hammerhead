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

/// <summary>
///
/// </summary>
public class N64GsLogoDecoder
{
    private const int POS_X = 24;
    private const int POS_Y = 40;

    private const int WIDTH = 320;
    private const int HEIGHT = 224;

    public Image<Rgba32> Decode(
        u8[] paletteBytes,
        u8[] imageBytes,
        bool transparency = false,
        Rgb24 transparentColor = new Rgb24()
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
    /// See
    /// https://developer.apple.com/documentation/accelerate/1642297-vimageconvert_rgba5551torgba8888
    /// </summary>
    /// <param name="rgb5Channel"></param>
    /// <returns></returns>
    private static u8 Rgb5ToRgb8(u8 rgb5Channel)
    {
        double x = (double)rgb5Channel * 255 / 31;
        return (u8)x;
    }
}
