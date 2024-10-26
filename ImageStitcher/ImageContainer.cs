using System.Windows.Media.Imaging;

namespace ImageStitcher;

public class ImageContainer
{
    public ImageContainer(string filePath)
    {
        Image.BeginInit();
        Image.CacheOption = BitmapCacheOption.OnLoad;
        Image.UriSource = new(filePath);
        Image.EndInit();
        Image.Freeze();
    }

    public BitmapImage Image { get; } = new();

    public string FilePath => Image.UriSource.OriginalString;

    public int Width => Image.PixelWidth;

    public int Height => Image.PixelHeight;
}
