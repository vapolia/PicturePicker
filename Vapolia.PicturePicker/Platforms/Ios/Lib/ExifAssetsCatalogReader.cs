//using AssetsLibrary;
//using Foundation;
//using System;
//using System.Threading.Tasks;
//using Vapolia.Mvvmcross.PicturePicker.Lib;

//namespace Vapolia.Mvvmcross.PicturePicker.Touch.Lib
//{
//    /// <summary>
//    /// Retreive Exif tag using AssetsCatalog on iOS for iOS version lower than 8
//    /// </summary>
//    [Preserve(AllMembers = true)]
//    public sealed class ExifAssetsCatalogReader : ExifReaderBase, IExifReader
//    {
//        /// <summary>
//        /// Get meta data dictionaries from AssetsCatalog
//        /// </summary>
//        /// <returns></returns>
//        public Task ReadExifTags(NSUrl assetUrl)
//        {
//            if (assetUrl == null) 
//                throw new ArgumentException("assetURL");

//            ALAssetsLibrary library = new ALAssetsLibrary();
//            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

//            library.AssetForUrl(assetUrl, asset =>
//            {
//                //Set metadata only if there is some
//                if (asset != null)
//                {
//                    //PixelHeight = (int)asset.DefaultRepresentation.Dimensions.Height;
//                    //PixelWidth = (int)asset.DefaultRepresentation.Dimensions.Width;

//                    try
//                    {
//                        var metadata = asset.DefaultRepresentation.Metadata;

//                        SetGlobalData(metadata);
//                        if(metadata[ImageIO.CGImageProperties.GPSDictionary] is NSDictionary gpsdico)
//                            SetGpsData(gpsdico);
//                        if(metadata[ImageIO.CGImageProperties.ExifDictionary] is NSDictionary exifdico)
//                            SetExifData(exifdico);
//                        if(metadata[ImageIO.CGImageProperties.TIFFDictionary] is NSDictionary tiffdico)
//                            SetTiffData(tiffdico);

//                        tcs.SetResult(0);
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine("Exif parsing failed : " + ex.ToString());
//                        tcs.SetException(ex);
//                        //Fail silently
//                    }
//                }
//                else
//                {
//                    tcs.SetResult(0);
//                }

//            }, error =>
//            {
//                tcs.SetException(new Exception(error.ToString()));
//            });
//            return tcs.Task;
//        }


//        public void Dispose()
//        {
//        }
//    }
//}
