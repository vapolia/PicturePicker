//using System;
//using System.Threading.Tasks;
//using CoreLocation;
//using Foundation;
//using Photos;
//using UIKit;
//using Vapolia.Mvvmcross.PicturePicker.Lib;

//namespace Vapolia.Mvvmcross.PicturePicker.Touch.Lib
//{
//    /// <summary>
//    /// This class is a shortcut to access the most used exif tag, it also take care of formating the Raw exif data like transforming latitude/longitude from int[3] to double
//    /// if a tag is missing use MediaFile TryGetRawTagValue to get your particular exif data.
//    /// </summary>
//    /// <remarks>
//    /// For a reference of ExifTags see : http://www.exiv2.org/tags.html
//    /// </remarks>
//    [Preserve(AllMembers = true)]
//    public class JpegInfoService : JpegInfoBase, IJpegInfo
//    {
//        public JpegInfo(NSUrl assetUrl)
//        {
//            if (assetUrl == null) 
//                throw new ArgumentNullException(nameof(assetUrl));
            
//            if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
//            {
//                _reader = new ExifPhotoKitReader(assetUrl);
//            }
//            else
//            {
//                //If iOS<8 use Assets Catalog
//                _reader = new ExifAssetsCatalogReader(assetUrl);
//            }
//        }

//        public JpegInfo(NSDictionary info)
//        {
//            if (info == null) throw new ArgumentNullException(nameof(info));
//            _reader = new ExifNSDictionnaryReader(info);
//        }

//        public Task TrySetGpsData(string ressourcePath, double latitude, double longitude, double altitude)
//        {
//            GpsLatitude = latitude;
//            GpsLongitude = longitude;
//            GpsAltitude = altitude;

//            if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
//            {
//                var loc = new CLLocation(new CLLocationCoordinate2D(GpsLatitude.Value, GpsLongitude.Value), GpsAltitude.Value, 1, 1, NSDate.Now);
//                return UpdateGpsMetaDataWithPhotoKit(NSUrl.FromString(ressourcePath), loc);
//            }

//            return Task.FromResult((object)null);
//        }

//        private Task UpdateGpsMetaDataWithPhotoKit(NSUrl image, CLLocation loc)
//        {
//            var assetResult = PHAsset.FetchAssetsUsingLocalIdentifiers(new [] { image.AbsoluteString }, null);
//            var a = (PHAsset)assetResult.firstObject;
            
//            if (a == null) 
//                throw new ArgumentException("NSURL does not point to an image in photokit");
         
//            var tcs = new TaskCompletionSource<int>();
//            PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(() => {
//                //build temp image
//                var req = PHAssetChangeRequest.ChangeRequest(a);
               
//                //This will update the photokit metadata database but not the file itself
//                req.Location = loc;
                
//            }, (success, error) =>
//            {
//                if (success)
//                    tcs.SetResult(0);
//                else
//                    tcs.SetException(new Exception(error.LocalizedDescription));
//            });
//            return tcs.Task;
//        }
//    }
//}
