namespace LibreShark.Hammerhead.Images;

/// <summary>
/// Based on
/// https://github.com/muthuspark/median-cut-color-quantization/blob/7d70ddb128822492e36d4d25780228c43d73b938/mcquantizer.py
/// </summary>
public static class MedianCutColorQuantization
{
    public static Image<Rgb24> ReduceColorPalette(Image<Rgb24> fullImage, int maxColors)
    {
        Image<Rgb24> reducedImage = fullImage.Clone();

        int[][] flattenedImgArray =
            Enumerable.Range(0, reducedImage.Width)
                .SelectMany(x => Enumerable.Range(0, reducedImage.Height)
                    .Select(y =>
                    {
                        Rgb24 pixel = reducedImage[x, y];
                        return new int[]
                        {
                            pixel.R,
                            pixel.G,
                            pixel.B,
                            x,
                            y,
                        };
                    }))
                .ToArray();

        SplitIntoBuckets(reducedImage, flattenedImgArray, maxColors);

        return reducedImage;
    }

    private static void SplitIntoBuckets(Image<Rgb24> image, int[][] rgbxyArray, int depth)
    {
        if (rgbxyArray.Length == 0)
            return;

        if (depth == 0 || rgbxyArray.Length == 1)
        {
            MedianCutQuantize(image, rgbxyArray);
            return;
        }

        int rRange = rgbxyArray.Max(R) - rgbxyArray.Min(R);
        int gRange = rgbxyArray.Max(G) - rgbxyArray.Min(G);
        int bRange = rgbxyArray.Max(B) - rgbxyArray.Min(B);

        int spaceWithHighestRange = 0;

        if (gRange >= rRange && gRange >= bRange)
            spaceWithHighestRange = 1;
        else if (bRange >= rRange && bRange >= gRange)
            spaceWithHighestRange = 2;
        else if (rRange >= bRange && rRange >= gRange)
            spaceWithHighestRange = 0;

        rgbxyArray = rgbxyArray.OrderBy(p => p[spaceWithHighestRange]).ToArray();
        int medianIndex = (rgbxyArray.Length + 1) / 2;

        // TODO(CheatoBaggins): Fix bug here
        SplitIntoBuckets(image, rgbxyArray.Take(medianIndex).ToArray(), depth - 1);
        SplitIntoBuckets(image, rgbxyArray.Skip(medianIndex).ToArray(), depth - 1);
    }

    private static void MedianCutQuantize(Image<Rgb24> image, int[][] rgbxyArray)
    {
        u8 rAverage = (u8)rgbxyArray.Average(R);
        u8 gAverage = (u8)rgbxyArray.Average(G);
        u8 bAverage = (u8)rgbxyArray.Average(B);

        foreach (int[] data in rgbxyArray)
        {
            int x = data[3];
            int y = data[4];
            image[x, y] = new Rgb24(rAverage, gAverage, bAverage);
        }
    }

    private static int R(int[] rgbxyArray)
    {
        return rgbxyArray[0];
    }

    private static int G(int[] rgbxyArray)
    {
        return rgbxyArray[1];
    }

    private static int B(int[] rgbxyArray)
    {
        return rgbxyArray[2];
    }
}
