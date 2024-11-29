using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using ByteImage = Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>;
using Image = Emgu.CV.Image<Emgu.CV.Structure.Gray, ushort>;
using Point = System.Windows.Point;

namespace ImageStitcher;

static class Stitcher
{
    public static void StitchImages(string sourceImagePath1, Rect roi1, string sourceImagePath2, Rect roi2, string resultImagePath)
    {
        var minWidth = Math.Min(roi1.Width, roi2.Width);
        var minHeight = Math.Min(roi1.Height, roi2.Height);
        roi1.Inflate((minWidth - roi1.Width) / 2, (minHeight - roi1.Height) / 2);
        roi2.Inflate((minWidth - roi2.Width) / 2, (minHeight - roi2.Height) / 2);

        using var image1 = new Image(sourceImagePath1);
        using var image2 = new Image(sourceImagePath2);
        using var subImage1 = image1.CropImage(roi1);
        using var subImage2 = image2.CropImage(roi2);
        using var subImage1Prepared = subImage1.PrepareImage(Settings.Default.Threshold);
        using var subImage2Prepared = subImage2.PrepareImage(Settings.Default.Threshold);
        PreviewRoiImages?.Invoke(subImage1Prepared, subImage2Prepared);

        var initialOffset = FindOffset(subImage1Prepared, subImage2Prepared);
        var (offset, angle) = FindOffsetAndAngle(subImage1Prepared, subImage2Prepared, initialOffset);
        var center = roi1.Location + (Vector)roi1.Size / 2;
        offset += roi2.Location - roi1.Location;
        var transform = GetTransform(angle, center, offset);
        transform.Invert();
        Point[] vertices = [new(0, 0), new(image2.Width - 1, 0), new(0, image2.Height - 1), new(image2.Width - 1, image2.Height - 1)];
        transform.Transform(vertices);
        vertices = [new(0, 0), new(image1.Width - 1, image1.Height - 1), .. vertices];
        var xMin = (int)Math.Floor(vertices.Min(point => point.X)) - 1;
        var yMin = (int)Math.Floor(vertices.Min(point => point.Y)) - 1;
        var xMax = (int)Math.Ceiling(vertices.Max(point => point.X)) + 1;
        var yMax = (int)Math.Ceiling(vertices.Max(point => point.Y)) + 1;
        transform.Translate(-xMin, -yMin);

        using var resultImage = image2.TransformImage(transform, xMax - xMin, yMax - yMin);
        using var subResultImage = resultImage.GetSubRect(new(-xMin, -yMin, image1.Width, image1.Height));
        image1.CopyTo(subResultImage);
        resultImage.Save(resultImagePath);
    }

    private static Matrix GetTransform(double angle, Point center, Vector offset)
    {
        var matrix = new Matrix();
        matrix.RotateAt(angle, center.X, center.Y);
        matrix.Translate(offset.X, offset.Y);
        return matrix;
    }

    private static Vector FindOffset(ByteImage image1, ByteImage image2)
    {
        using var detector = new ORB();
        using var keyPoints1 = new VectorOfKeyPoint();
        using var keyPoints2 = new VectorOfKeyPoint();
        using var descriptors1 = new Mat();
        using var descriptors2 = new Mat();
        detector.DetectAndCompute(image1, null, keyPoints1, descriptors1, false);
        detector.DetectAndCompute(image2, null, keyPoints2, descriptors2, false);

        using var matcher = new BFMatcher(DistanceType.Hamming);
        using var matches = new VectorOfDMatch();
        matcher.Match(descriptors1, descriptors2, matches);

        var offsets = matches.ToArray().Select(match =>
        {
            var point1 = keyPoints1[match.QueryIdx].Point;
            var point2 = keyPoints2[match.TrainIdx].Point;
            return new Vector(point2.X - point1.X, point2.Y - point1.Y);
        }).ToList();
        return FindOffset2(offsets);
    }

    private static Vector FindOffset(List<Vector> offsets)
    {
        var offsetsX = offsets.Select(offset => offset.X).ToList();
        offsetsX.Sort();
        var offsetsY = offsets.Select(offset => offset.Y).ToList();
        offsetsY.Sort();
        return new(offsetsX[offsetsX.Count / 2], offsetsY[offsetsY.Count / 2]);
    }

    private static Vector FindOffset2(List<Vector> offsets)
    {
        var xMin = offsets.Min(offset => offset.X);
        var yMin = offsets.Min(offset => offset.Y);
        var xMax = offsets.Max(offset => offset.X);
        var yMax = offsets.Max(offset => offset.Y);
        var xCount = (int)(xMax + 1 - xMin) / 10 + 1;
        var yCount = (int)(yMax + 1 - yMin) / 10 + 1;
        var histogram = new int[xCount, yCount];
        offsets.ForEach(offset => ++histogram[(int)((offset.X - xMin) / 10), (int)((offset.Y - yMin) / 10)]);
        var maxCount = histogram.Cast<int>().Max();
        for (var i = 0; i < xCount; ++i)
        {
            for (var j = 0; j < yCount; ++j)
            {
                if (histogram[i, j] == maxCount)
                {
                    return new(xMin + i * 10 + 5, yMin + j * 10 + 5);
                }
            }
        }
        return new(0, 0);
    }

    private static (Vector, double) FindOffsetAndAngle(ByteImage image1, ByteImage image2, Vector initialOffset)
    {
        double calculateDiff(Vector<double> v)
        {
            var x = (int)v[0];
            var y = (int)v[1];
            var angle = v[2];
            Trace.WriteLine($"[{x}, {y}, {angle}]");

            var transform = GetTransform(angle, new(image1.Width / 2, image2.Height / 2), new(x, y));
            using var image1Transformed = image1.TransformImage(transform, image1.Width, image1.Height);
            using var imageDiff = image2.AbsDiff(image1Transformed);
            var diff = imageDiff.GetSum().Intensity;
            diff /= imageDiff.Width * imageDiff.Height;
            return diff;
        }

        var optimizer = new NelderMeadSimplex(1e-10, 1000);
        var initialGuess = Vector<double>.Build.DenseOfArray([initialOffset.X, initialOffset.Y, 0]);
        var initialPertubation = Vector<double>.Build.DenseOfArray([10, 10, 90]);
        var result = optimizer.FindMinimum(ObjectiveFunction.Value(calculateDiff), initialGuess, initialPertubation);
        return (new(result.MinimizingPoint[0], result.MinimizingPoint[1]), result.MinimizingPoint[2]);
    }

    public static event Action<ByteImage, ByteImage> PreviewRoiImages;
}
