using CoreGraphics;
using UIKit;

namespace Vapolia.Mvvmcross.PicturePicker.Touch
{
    public static class ImageHelper
    {
        //public static UIImage FromFile(string filename, CGSize fitSize)
        //{
        //    var imageFile = UIImage.FromFile(filename);
        //    var image = imageFile.ImageToFitSize(fitSize);
        //    imageFile.Dispose();
        //    return image;
        //}

        public static UIImage ImageToFitSize(this UIImage image, CGSize fitSize)
        {
            var size = image.Size;

            double width = size.Width;
            double height = size.Height;
            if ((fitSize.Width > 0 && width > fitSize.Width) || (fitSize.Height > 0 && height > fitSize.Height))
                Scale(ref width, ref height, fitSize.Width, fitSize.Height);
            else
                return image;

            //var loImageOriginalSource = CGImageSource.FromData(loDataFotoOriginal);
            //var loDicMetadata = loImageOriginalSource.CopyProperties(new CGImageOptions());

            var destRect = new CGRect(0,0,width,height);
            UIGraphics.BeginImageContextWithOptions(destRect.Size, true, 0);
            image.Draw(destRect);
            var newImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            image.Dispose();

            //CGImageDestinationAddImage(destination, cgImage, metadata)

            return newImage;
        }

        internal static void Scale(ref double width, ref double height, double fillWidthPixels=0, double fillHeightPixels=0)
	    {
	        var hasFillWidth = fillWidthPixels > float.Epsilon;
            var hasFillHeight = fillHeightPixels > float.Epsilon;

            if (hasFillWidth || hasFillHeight)
            {
                var scaleX = hasFillWidth ? fillWidthPixels/width : 1;
                var scaleY = hasFillHeight ? fillHeightPixels/height : 1;

                if (hasFillWidth && hasFillHeight)
                {
                    var aspect = height / width;
                    var aspectDisplay = fillHeightPixels / fillWidthPixels;

                    if (aspectDisplay < aspect)
                    {
                        var newHeight = fillWidthPixels * aspect;
                        if (newHeight > fillHeightPixels)
                        {
                            newHeight = fillHeightPixels;
                            var newWidth = newHeight / aspect;
                            width = newWidth;
                        }
                        else
                            width = fillWidthPixels;

                        height = newHeight;
                    }
                    else
                    {
                        var newWidth = fillHeightPixels / aspect;
                        if (newWidth > fillWidthPixels)
                        {
                            newWidth = fillWidthPixels;
                            var newHeight = newWidth * aspect;
                            height = newHeight;
                        }
                        else
                            height = fillHeightPixels;

                        width = newWidth;
                    }
                    return;
                }

                var scale = hasFillWidth ? scaleX : scaleY;
                width *= scale;
                height *= scale;
            }
	    }


        //internal static async Task SaveImage(string filePath, Stream stream, CancellationToken cancel)
        //{
        //    if (File.Exists(filePath))
        //        File.Delete(filePath);

        //    using (var fileStream = File.OpenWrite(filePath))
        //        await stream.CopyToAsync(fileStream, 81920, cancel);
        //}
    }
}
