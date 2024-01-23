//using CoreImage;
//using Foundation;
//using Photos;
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Vapolia.Mvvmcross.PicturePicker.Lib;

//namespace Vapolia.Mvvmcross.PicturePicker.Touch.Lib
//{
//    public class MediaExifException : Exception
//    {
//        public MediaExifException(string message) : base(message) {}
//    }

//    public class MediaFileNotFoundException : Exception
//    {
//        public MediaFileNotFoundException(string message) : base(message) {}
//    }

//    /// <summary>
//    /// Retreive Exif tag using PhotoKit on iOS for iOS version >= 8
//    /// </summary>
//    public sealed class ExifPhotoKitReader : ExifReaderBase, IExifReader
//    {
//        private NSUrl _assetURL;

//        //public int PixelWidth
//        //{ get; private set; }

//        //public int PixelHeight
//        //{ get; private set; }

//        public ExifPhotoKitReader(NSUrl assetUrl)
//        {
//            if (assetUrl == null) 
//                throw new ArgumentException(nameof(assetUrl));

//            _assetURL = assetUrl;
//            ReadExifTags().ContinueWith(task =>
//            {
//                if (!task.IsCompleted)
//                    throw new MediaExifException("Could not read exif data from photokit");
//            });
//        }

//        /// <summary>
//        /// Get meta data dictionaries from PhotoKit
//        /// </summary>
//        /// <returns></returns>
//        private Task ReadExifTags()
//        {
//            var tcs = new TaskCompletionSource<int>();
//            try
//            {
//                var res = PHAsset.FetchAssets(new [] { _assetURL }, new PHFetchOptions());
               
//                //Since we selected only one pic there should be only one item in the list
//                var photo = (PHAsset)res.FirstOrDefault();
//                if (photo == null)
//                {
//                    tcs.SetException(new MediaFileNotFoundException(_assetURL.ToString()));
//                    return tcs.Task;
//                }

//                //PixelWidth = (int) photo.PixelWidth;
//                //PixelHeight = (int)photo.PixelHeight;
             
//                if (PHPhotoLibrary.AuthorizationStatus != PHAuthorizationStatus.Authorized)
//                {
//                    PHPhotoLibrary.RequestAuthorization(status =>
//                    {
//                        if (status != PHAuthorizationStatus.Authorized)
//                            tcs.SetCanceled();
//                    });
//                }

//                photo.RequestContentEditingInput(new PHContentEditingInputRequestOptions { NetworkAccessAllowed = false }, (input, options) =>
//                {
//                    try
//                    {
//                        //Get the orginial image (FullSize)
//                        var img = CIImage.FromUrl(input.FullSizeImageUrl);
//                        var prop = img.Properties;
                        
//                        SetGlobalData(prop.Dictionary);
//                        //si il y a des tags GPS au sein du fichier
//                        if (prop.Gps != null)
//                            SetGpsData(prop.Gps.Dictionary);
//                        else
//                        {
//                            //Sinon on essaye de recuperer ces valeurs GPS au sein de la BD photoKit
//                            if(photo.Location != null)
//                            {
//                                SetGpsData(photo.Location);
//                            }
//                        }
//                        if (prop.Exif != null)
//                            SetExifData(prop.Exif.Dictionary);
//                        if (prop.Tiff != null)
//                            SetTiffData(prop.Tiff.Dictionary);

//                        tcs.SetResult(0);

//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine("Exif parsing failed : " + ex.ToString());
//                        tcs.SetException(ex);
//                    }
//                });

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.ToString());
//                tcs.TrySetException(ex);
//            }

//            return tcs.Task;
//        }

//        public void Dispose()
//        {
//        }
//    }
//}
