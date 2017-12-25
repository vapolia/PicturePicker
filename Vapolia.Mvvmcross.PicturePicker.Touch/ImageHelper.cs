using System;
using System.IO;
using System.Threading;
using CoreGraphics;
using System.Threading.Tasks;
using UIKit;

namespace Vapolia.Mvvmcross.PicturePicker.Touch
{
    public static class ImageHelper
    {
        public static UIImage FromFile(string filename, CGSize fitSize)
        {
            var imageFile = UIImage.FromFile(filename);

            return imageFile.ImageToFitSize(fitSize);
        }

        public static UIImage ImageToFitSize(this UIImage image, CGSize fitSize)
        {
            var imageScaleFactor = 1.0;
            imageScaleFactor = (float)image.CurrentScale;

            var sourceWidth = image.Size.Width*imageScaleFactor;
            var sourceHeight = image.Size.Height*imageScaleFactor;
            var targetWidth = fitSize.Width;
            var targetHeight = fitSize.Height;

            var sourceRatio = sourceWidth/sourceHeight;
            var targetRatio = targetWidth/targetHeight;

            var scaleWidth = (sourceRatio <= targetRatio);
            scaleWidth = !scaleWidth;

            double scalingFactor;
            double scaledWidth;
            double scaledHeight;

            if (scaleWidth)
            {
                scalingFactor = 1.0/sourceRatio;
                scaledWidth = targetWidth;
                scaledHeight = Math.Round(targetWidth*scalingFactor);
            }
            else
            {
                scalingFactor = sourceRatio;
                scaledWidth = Math.Round(targetHeight*scalingFactor);
                scaledHeight = targetHeight;
            }

            var destRect = new CGRect(0, 0, (float) scaledWidth, (float) scaledHeight);

            UIGraphics.BeginImageContextWithOptions(destRect.Size, true, 0f);
            image.Draw(destRect);
            var newImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            return newImage;
        }

        internal static async Task SaveImage(string filePath, Stream stream, CancellationToken cancel)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var fileStream = File.OpenWrite(filePath))
                await stream.CopyToAsync(fileStream, 81920, cancel);
        }
    }
}
