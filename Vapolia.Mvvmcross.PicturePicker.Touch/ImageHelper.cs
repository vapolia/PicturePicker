using System;
using System.Collections.Generic;
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

        internal static async Task<string> SaveImage(FileService fs, Stream stream, CancellationToken cancel)
        {
            //Save to file
            var fileName = Guid.NewGuid() + ".tmp.jpg";
            await fs.WriteFileAsync(fileName, stream, cancel);
            return fileName;
        }

        internal static async Task<string> SaveImage(FileService fs, byte[] bytes, CancellationToken cancel)
        {
            var fileName = Guid.NewGuid() + ".tmp.jpg";
            await fs.WriteFileAsync(fileName, bytes, cancel);
            return fileName;
        }
    }
}
