using Emgu.CV;
using Emgu.CV.Stitching;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ImageStitcher
{
    static class Program
    {
        static void Main(string[] args)
        {
            var filePaths = Directory.GetFiles(args[0], "*.tif");
            var images = filePaths.Select(filePath => new Image<Rgb, byte>(filePath)).ToList();
            using var resultImage = new Image<Rgb, byte>(1000, 1000);

            Console.Write("Stitching...");
            var stopwatch = Stopwatch.StartNew();
            StitchImages(images, resultImage);
            stopwatch.Stop();
            Console.WriteLine($" {stopwatch.ElapsedMilliseconds} ms");

            resultImage.Save("result.tif");

            foreach (var image in images)
            {
                image.Dispose();
            }
        }

        private static void StitchImages(List<Image<Rgb, byte>> images, Image<Rgb, byte> resultImage)
        {
            using var stitcher = new Stitcher(Stitcher.Mode.Scans);
            stitcher.Stitch(new VectorOfMat(images.Select(image => image.Mat).ToArray()), resultImage.Mat);
        }
    }
}
