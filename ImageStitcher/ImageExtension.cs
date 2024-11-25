using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Windows;
using System.Windows.Media;

namespace ImageStitcher;

static class ImageExtension
{
    public static Image<Gray, T> CropImage<T>(this Image<Gray, T> image, Rect rect)
        where T : new() => image.GetSubRect(new((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));

    public static Image<Gray, byte> PrepareImage(this Image<Gray, ushort> image, int threshold)
    {
        using var byteImage = image.Convert<Gray, byte>();
        var binaryImage = new Image<Gray, byte>(byteImage.Size);
        CvInvoke.Threshold(byteImage, binaryImage, threshold, 255, ThresholdType.Binary);
        return binaryImage;
    }

    public static Image<Gray, T> TransformImage<T>(this Image<Gray, T> image, Matrix transform, int width, int height)
        where T : new()
    {
        using var transformMat = new Mat(2, 3, DepthType.Cv64F, 1);
        transformMat.SetTo([transform.M11, transform.M21, transform.OffsetX, transform.M12, transform.M22, transform.OffsetY]);
        return image.WarpAffine(transformMat, width, height, Inter.Cubic, 0, 0, new());
    }
}
