using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using MvvmCross.Platform;
using MvvmCross.Platform.Droid;
using MvvmCross.Platform.Droid.Platform;
using MvvmCross.Platform.Droid.Views;
using MvvmCross.Platform.Exceptions;
using MvvmCross.Platform.Platform;
using MvvmCross.Plugins;
using MvvmCross.Plugins.File;
using Uri = Android.Net.Uri;

namespace Vapolia.Mvvmcross.PicturePicker.Droid
{
    [Preserve(AllMembers = true)]
    public class PicturePicker : MvxAndroidTask, IPicturePicker
    {
        private Uri cachedUriLocation;
        private RequestParameters currentRequestParameters;

        public Task<string> ChoosePictureFromLibrary(Action<Task<bool>> saving = null, int maxPixelDimension = 0, int percentQuality = 80)
        {
            //throw new NotImplementedException();

            var intent = new Intent(Intent.ActionGetContent);
            intent.SetType("image/*");
            var tcs = new TaskCompletionSource<string>();
            ChoosePictureCommon(MvxIntentRequestCode.PickFromFile, intent, maxPixelDimension, percentQuality,
                stream =>
                {
                    var fs = Mvx.Resolve<IMvxFileStore>();
                    var fileName = Guid.NewGuid() + ".tmp";
                    fs.WriteFile(fileName, outstream => stream.CopyTo(outstream, 64000));

                    tcs.SetResult(fileName);
                }, () => tcs.SetResult(null));
            return tcs.Task;
        }

        public Task<string> TakePicture(Action<Task<bool>> saving = null, int maxPixelDimension = 0, int percentQuality = 0, bool useFrontCamera = false)
        {
            //throw new NotImplementedException();

            var intent = new Intent(MediaStore.ActionImageCapture);

            cachedUriLocation = GetNewImageUri();
            intent.PutExtra(MediaStore.ExtraOutput, cachedUriLocation);
            intent.PutExtra("outputFormat", Bitmap.CompressFormat.Jpeg.ToString());
            intent.PutExtra("return-data", true);
            var tcs = new TaskCompletionSource<string>();

            ChoosePictureCommon(MvxIntentRequestCode.PickFromCamera, intent, maxPixelDimension, percentQuality,
                stream =>
            {
                var fs = Mvx.Resolve<IMvxFileStore>();
                var fileName = Guid.NewGuid() + ".tmp";
                fs.WriteFile(fileName, outstream => stream.CopyTo(outstream, 64000));

                tcs.SetResult(fileName);
            }, () => tcs.SetResult(null));
            return tcs.Task;
        }

        private Uri GetNewImageUri()
        {
            // Optional - specify some metadata for the picture
            var contentValues = new ContentValues();
            //contentValues.Put(MediaStore.Images.ImageColumnsConsts.Description, "A camera photo");

            // Specify where to put the image
            return
                Mvx.Resolve<IMvxAndroidGlobals>()
                    .ApplicationContext.ContentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, contentValues);
        }

        public void ChoosePictureCommon(MvxIntentRequestCode pickId, Intent intent, int maxPixelDimension,
                                        int percentQuality, Action<Stream> pictureAvailable, Action assumeCancelled)
        {
            if (currentRequestParameters != null)
                throw new MvxException("Cannot request a second picture while the first request is still pending");

            currentRequestParameters = new RequestParameters(maxPixelDimension, percentQuality, pictureAvailable,
                                                              assumeCancelled);
            StartActivityForResult((int) pickId, intent);
        }

        protected override void ProcessMvxIntentResult(MvxIntentResultEventArgs result)
        {
            MvxTrace.Trace("ProcessMvxIntentResult started...");

            Uri uri;

            switch ((MvxIntentRequestCode) result.RequestCode)
            {
                case MvxIntentRequestCode.PickFromFile:
                    uri = result.Data?.Data;
                    break;
                case MvxIntentRequestCode.PickFromCamera:
                    uri = cachedUriLocation;
                    break;
                default:
                    // ignore this result - it's not for us
                    MvxTrace.Trace("Unexpected request received from MvxIntentResult - request was {0}",
                                   result.RequestCode);
                    return;
            }

            ProcessPictureUri(result, uri);
        }

        private void ProcessPictureUri(MvxIntentResultEventArgs result, Uri uri)
        {
            if (currentRequestParameters == null)
            {
                MvxTrace.Error("Internal error - response received but _currentRequestParameters is null");
                return; // we have not handled this - so we return null
            }

            var responseSent = false;
            try
            {
                // Note for furture maintenance - it might be better to use var outputFileUri = data.GetParcelableArrayExtra("outputFileuri") here?
                if (result.ResultCode != Result.Ok)
                {
                    MvxTrace.Trace("Non-OK result received from MvxIntentResult - {0} - request was {1}",
                                   result.ResultCode, result.RequestCode);
                    return;
                }

                if (string.IsNullOrEmpty(uri?.Path))
                {
                    MvxTrace.Trace("Empty uri or file path received for MvxIntentResult");
                    return;
                }

                MvxTrace.Trace("Loading InMemoryBitmap started...");
                var memoryStream = LoadInMemoryBitmap(uri);
                if (memoryStream == null)
                {
                    MvxTrace.Trace("Loading InMemoryBitmap failed...");
                    return;
                }
                MvxTrace.Trace("Loading InMemoryBitmap complete...");
                responseSent = true;
                MvxTrace.Trace("Sending pictureAvailable...");
                currentRequestParameters.PictureAvailable(memoryStream);
                MvxTrace.Trace("pictureAvailable completed...");
            }
            finally
            {
                if (!responseSent)
                    currentRequestParameters.AssumeCancelled();

                currentRequestParameters = null;
            }
        }

        private MemoryStream LoadInMemoryBitmap(Uri uri)
        {
            var memoryStream = new MemoryStream();
            var bitmap = LoadScaledBitmap(uri);
            if (bitmap == null)
                return null;
            using (bitmap)
            {
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, currentRequestParameters.PercentQuality, memoryStream);
            }
            memoryStream.Seek(0L, SeekOrigin.Begin);
            return memoryStream;
        }

        private Bitmap LoadScaledBitmap(Uri uri)
        {
            ContentResolver contentResolver = Mvx.Resolve<IMvxAndroidGlobals>().ApplicationContext.ContentResolver;
            var maxDimensionSize = GetMaximumDimension(contentResolver, uri);
            Bitmap sampled;
            if (currentRequestParameters.MaxPixelDimension != 0)
            {
                var sampleSize = (int)Math.Ceiling((maxDimensionSize) /
                                                    ((double)currentRequestParameters.MaxPixelDimension));
                if (sampleSize < 1)
                {
                    // this shouldn't happen, but if it does... then trace the error and set sampleSize to 1
                    MvxTrace.Trace(
                        "Warning - sampleSize of {0} was requested - how did this happen - based on requested {1} and returned image size {2}",
                        sampleSize,
                        currentRequestParameters.MaxPixelDimension,
                        maxDimensionSize);
                    // following from https://github.com/MvvmCross/MvvmCross/issues/565 we return null in this case
                    // - it suggests that Android has returned a corrupt image uri
                    return null;
                }
                sampled = LoadResampledBitmap(contentResolver, uri, sampleSize);
            }
            else
            {
                using (var inputStream = contentResolver.OpenInputStream(uri))
                {
                    var optionsDecode = new BitmapFactory.Options();

                    sampled = BitmapFactory.DecodeStream(inputStream, null, optionsDecode);
                }
            }
            try
            {
                var rotated = ExifRotateBitmap(contentResolver, uri, sampled);
                return rotated;
            }
            catch (Exception pokemon)
            {
                Mvx.Trace("Problem seem in Exit Rotate {0}", pokemon.ToLongString());
                return sampled;
            }
        }

        private Bitmap LoadResampledBitmap(ContentResolver contentResolver, Uri uri, int sampleSize)
        {
            using (var inputStream = contentResolver.OpenInputStream(uri))
            {
                var optionsDecode = new BitmapFactory.Options {InSampleSize = sampleSize};

                return BitmapFactory.DecodeStream(inputStream, null, optionsDecode);
            }
        }

        private static int GetMaximumDimension(ContentResolver contentResolver, Uri uri)
        {
            using (var inputStream = contentResolver.OpenInputStream(uri))
            {
                var optionsJustBounds = new BitmapFactory.Options
                    {
                        InJustDecodeBounds = true
                    };
                // ReSharper disable once UnusedVariable
                var metadataResult = BitmapFactory.DecodeStream(inputStream, null, optionsJustBounds);
                var maxDimensionSize = Math.Max(optionsJustBounds.OutWidth, optionsJustBounds.OutHeight);
                return maxDimensionSize;
            }
        }

        private Bitmap ExifRotateBitmap(ContentResolver contentResolver, Uri uri, Bitmap bitmap)
        {
            if (bitmap == null)
                return null;

            var exif = new Android.Media.ExifInterface(GetRealPathFromUri(contentResolver, uri));
            var rotation = exif.GetAttributeInt(Android.Media.ExifInterface.TagOrientation, (Int32)Android.Media.Orientation.Normal);
            var rotationInDegrees = ExifToDegrees(rotation);
            if (rotationInDegrees == 0)
                return bitmap;

            using (var matrix = new Matrix())
            {
                matrix.PreRotate(rotationInDegrees);
                return Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true);
            }
        }

        private string GetRealPathFromUri(ContentResolver contentResolver, Uri uri)
        {
            var proj = new [] { MediaStore.Images.ImageColumns.Data };
            using (var cursor = contentResolver.Query(uri, proj, null, null, null))
            {
                var columnIndex = cursor.GetColumnIndexOrThrow(MediaStore.Images.ImageColumns.Data);
                cursor.MoveToFirst();
                return cursor.GetString(columnIndex);
            }
        }

        private static int ExifToDegrees(int exifOrientation)
        {
            switch (exifOrientation)
            {
                case (int)Android.Media.Orientation.Rotate90:
                    return 90;
                case (int)Android.Media.Orientation.Rotate180:
                    return 180;
                case (int)Android.Media.Orientation.Rotate270:
                    return 270;
            }

            return 0;
        }

        #region Nested type: RequestParameters

        private class RequestParameters
        {
            public RequestParameters(int maxPixelDimension, int percentQuality, Action<Stream> pictureAvailable,
                                     Action assumeCancelled)
            {
                PercentQuality = percentQuality;
                MaxPixelDimension = maxPixelDimension;
                AssumeCancelled = assumeCancelled;
                PictureAvailable = pictureAvailable;
            }

            public Action<Stream> PictureAvailable { get; }
            public Action AssumeCancelled { get; }
            public int MaxPixelDimension { get; }
            public int PercentQuality { get; }
        }

        #endregion
    }
}
