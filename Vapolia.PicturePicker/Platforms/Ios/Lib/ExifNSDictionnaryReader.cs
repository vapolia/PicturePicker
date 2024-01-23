//using Foundation;
//using UIKit;
//using Vapolia.Mvvmcross.PicturePicker.Lib;

//namespace Vapolia.Mvvmcross.PicturePicker.Touch.Lib
//{
//    public class ExifNSDictionnaryReader : ExifReaderBase, IExifReader
//    {
//        //public int PixelWidth { get; }

//        //public int PixelHeight { get; }

//        public ExifNSDictionnaryReader(NSDictionary dico)
//        {
//            if (dico.ValueForKey(new NSString("UIImagePickerControllerMediaMetadata")) is NSDictionary msMeta)
//            {
//                var meta = new NSMutableDictionary(msMeta);
//                SetGlobalData(meta);

//                if (meta.ValueForKey(ImageIO.CGImageProperties.ExifDictionary) is NSDictionary exifDic)
//                    SetExifData(new NSMutableDictionary(exifDic));

//                if (meta.ValueForKey(ImageIO.CGImageProperties.TIFFDictionary) is NSDictionary tiffDic)
//                    SetTiffData(new NSMutableDictionary(tiffDic));

//                if (meta.ValueForKey(ImageIO.CGImageProperties.GPSDictionary) is NSDictionary gpsDic)
//                    SetGpsData(new NSMutableDictionary(gpsDic));
//            }

//            //if (dico.ValueForKey(new NSString("UIImagePickerControllerOriginalImage")) is UIImage photo)
//            //{
//            //    PixelHeight = (int)photo.Size.Height;
//            //    PixelWidth = (int)photo.Size.Width;
//            //}
//        }

//        public void Dispose()
//        {
//        }
//    }
//}
