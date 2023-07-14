using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;

namespace LibreShark.Hammerhead.IO;

public class EmbeddedImage
{
    public readonly string FileName;
    public readonly Image<Rgba32> Image;

    public EmbeddedImage(string fileName, Image<Rgba32> image)
    {
        FileName = fileName;
        Image = image;

        // TODO(CheatoBaggins): Move this to an Images class
        var fonts = new FontCollection();
        FontFamily family = fonts.Add(new MemoryStream(Resources.FONT_PIXELMIX_STANDARD));
        Font font = family.CreateFont(8, FontStyle.Regular);
        TextOptions textOptions = new(font)
        {
            Origin = new PointF(292, 134),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextAlignment = TextAlignment.End,

        };
        IBrush brush = Brushes.Solid(Color.White);
        IPen pen = Pens.Solid(Color.Black, 1);
        string text = "v4.99\n2023-07-14";

        // Draws the text with horizontal red and blue hatching with a dash dot pattern outline.
        image.Mutate(x => x.DrawText(textOptions, text, brush, pen));
    }
}
