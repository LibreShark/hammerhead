using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

namespace LibreShark.Hammerhead.Images;

public static class ColorQuantizer
{
    public static Image<Rgb24> ReduceColorPalette(Image<Rgb24> image, int maxColors)
    {
        string tempFileName = Path.GetTempFileName();
        image.SaveAsGif(tempFileName, new GifEncoder()
        {
            Quantizer = new OctreeQuantizer() { Options = { MaxColors = maxColors, Dither = null } },
        });
        return Image.Load<Rgb24>(tempFileName);
    }
}
