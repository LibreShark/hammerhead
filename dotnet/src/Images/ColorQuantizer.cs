using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

namespace LibreShark.Hammerhead.Images;

public static class ColorQuantizer
{
    public static Image<Rgb24> ReduceColorPalette(Image<Rgb24> image, int maxColors)
    {
        string tempFileName = Path.GetTempFileName();
        var colors = new HashSet<Rgb24>();
        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                colors.Add(image[x, y]);
            }
        }

        if (colors.Count <= maxColors)
        {
            return image.Clone();
        }

        image.SaveAsGif(tempFileName, new GifEncoder()
        {
            Quantizer = new OctreeQuantizer() { Options = { MaxColors = maxColors, Dither = null } },
        });
        return Image.Load<Rgb24>(tempFileName);
    }
}
