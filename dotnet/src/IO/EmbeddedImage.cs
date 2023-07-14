namespace LibreShark.Hammerhead.IO;

public class EmbeddedImage
{
    public readonly string FileName;
    public readonly Image<Rgba32> Image;

    public EmbeddedImage(string fileName, Image<Rgba32> image)
    {
        FileName = fileName;
        Image = image;
    }
}
