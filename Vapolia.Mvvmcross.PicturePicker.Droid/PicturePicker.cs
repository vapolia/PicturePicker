using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using MvvmCross.Platform.Droid;
using MvvmCross.Platform.Droid.Platform;
using MvvmCross.Platform.Droid.Views;
using MvvmCross.Platform.Exceptions;
using MvvmCross.Platform.Platform;
using Uri = Android.Net.Uri;
using Android.Runtime;
using MvvmCross.Platform.Logging;
using Stream = System.IO.Stream;

namespace Vapolia.Mvvmcross.PicturePicker.Droid
{
    [Preserve(AllMembers = true)]
    public class PicturePicker : MvxAndroidTask, IPicturePicker
    {
        private readonly IMvxLog log;
        private readonly IMvxAndroidGlobals androidGlobals;
        private Uri cachedUriLocation;
        private RequestParameters currentRequestParameters;

        public PicturePicker(IMvxLog logger, IMvxAndroidGlobals androidGlobals)
        {
            log = logger;
            this.androidGlobals = androidGlobals;
        }

        public Task<bool> ChoosePictureFromLibrary(string filePath, Action<Task<bool>> saving = null, int maxPixelDimension = 0, int percentQuality = 80)
        {
            var intent = new Intent(Intent.ActionGetContent);
            intent.SetType("image/*");
            var tcs = new TaskCompletionSource<bool>();
            ChoosePictureCommon(MvxIntentRequestCode.PickFromFile, intent, maxPixelDimension, percentQuality,
                async stream =>
                {
                    await SaveImage(filePath, stream, CancellationToken.None);
                    tcs.SetResult(true);
                }, () => tcs.SetResult(false));
            return tcs.Task;
        }

        public bool HasCamera => Application.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);

        public Task<bool> TakePicture(string filePath, Action<Task<bool>> saving = null, int maxPixelDimension = 0, int percentQuality = 0, bool useFrontCamera = false)
        {
            var intent = new Intent(MediaStore.ActionImageCapture);

            cachedUriLocation = GetNewImageUri();
            intent.PutExtra(MediaStore.ExtraOutput, cachedUriLocation);
            intent.PutExtra("outputFormat", Bitmap.CompressFormat.Jpeg.ToString());
            intent.PutExtra("return-data", true);
            if (useFrontCamera && Application.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFront))
            {
                intent.PutExtra("android.intent.extras.CAMERA_FACING", (int)Android.Hardware.CameraFacing.Front); //lower than LOLLIPOP_MR1
                intent.PutExtra("android.intent.extras.LENS_FACING_FRONT", 1); //LOLLIPOP_MR1 or greater, except Android7
                intent.PutExtra("android.intent.extra.USE_FRONT_CAMERA", true); //Android7
            }

            var tcs = new TaskCompletionSource<bool>();
            ChoosePictureCommon(MvxIntentRequestCode.PickFromCamera, intent, maxPixelDimension, percentQuality,
                async stream =>
                {
                    await SaveImage(filePath, stream, CancellationToken.None);
                    tcs.SetResult(true);
                }, () => tcs.SetResult(false));
            return tcs.Task;
        }

        private async Task SaveImage(string filePath, Stream stream, CancellationToken cancel)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var fileStream = File.OpenWrite(filePath))
                await stream.CopyToAsync(fileStream, 81920, cancel);
        }

        private Uri GetNewImageUri()
        {
            // Optional - specify some metadata for the picture
            var contentValues = new ContentValues();
            //contentValues.Put(MediaStore.Images.ImageColumnsConsts.Description, "A camera photo");

            // Specify where to put the image
            return androidGlobals.ApplicationContext.ContentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, contentValues);
        }

        public void ChoosePictureCommon(MvxIntentRequestCode pickId, Intent intent, int maxPixelDimension,
                                        int percentQuality, Action<Stream> pictureAvailable, Action assumeCancelled)
        {
            if (currentRequestParameters != null)
                throw new MvxException("Cannot request a second picture while the first request is still pending");

            currentRequestParameters = new RequestParameters(maxPixelDimension, percentQuality, pictureAvailable, assumeCancelled);
            StartActivityForResult((int)pickId, intent);
        }

        protected override void ProcessMvxIntentResult(MvxIntentResultEventArgs result)
        {
            log.Trace("ProcessMvxIntentResult started...");

            Uri uri;

            switch ((MvxIntentRequestCode)result.RequestCode)
            {
                case MvxIntentRequestCode.PickFromFile:
                    uri = result.Data?.Data;
                    break;
                case MvxIntentRequestCode.PickFromCamera:
                    uri = cachedUriLocation;
                    break;
                default:
                    // ignore this result - it's not for us
                    log.Trace("Unexpected request received from MvxIntentResult - request was {0}",
                                    result.RequestCode);
                    return;
            }

            //Do heavy work in a worker thread
            Task.Run(() => ProcessPictureUri(result, uri));
        }

        private void ProcessPictureUri(MvxIntentResultEventArgs result, Uri uri)
        {
            if (currentRequestParameters == null)
            {
                log.Error("Internal error - response received but _currentRequestParameters is null");
                return; // we have not handled this - so we return null
            }

            var responseSent = false;
            try
            {
                // Note for furture maintenance - it might be better to use var outputFileUri = data.GetParcelableArrayExtra("outputFileuri") here?
                if (result.ResultCode != Result.Ok)
                {
                    log.Trace("Non-OK result received from MvxIntentResult - {0} - request was {1}",
                                    result.ResultCode, result.RequestCode);
                    return;
                }

                if (string.IsNullOrEmpty(uri?.Path))
                {
                    log.Trace("Empty uri or file path received for MvxIntentResult");
                    return;
                }

                log.Trace("Loading InMemoryBitmap started...");
                var memoryStream = LoadInMemoryBitmap(uri);
                if (memoryStream == null)
                {
                    log.Trace("Loading InMemoryBitmap failed...");
                    return;
                }
                log.Trace("Loading InMemoryBitmap complete...");
                responseSent = true;
                log.Trace("Sending pictureAvailable...");
                currentRequestParameters.PictureAvailable(memoryStream);
                log.Trace("pictureAvailable completed...");
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
            var contentResolver = androidGlobals.ApplicationContext.ContentResolver;
            var maxDimensionSize = GetMaximumDimension(contentResolver, uri);

            Bitmap sampled;
            if (currentRequestParameters.MaxPixelDimension != 0)
            {
                var sampleSize = (int)Math.Ceiling(maxDimensionSize / (double)currentRequestParameters.MaxPixelDimension);
                if (sampleSize < 1)
                {
                    // this shouldn't happen, but if it does... then trace the error and set sampleSize to 1
                    log.Trace(
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
                return ExifRotateBitmap(contentResolver, uri, sampled);
            }
            catch (Exception e)
            {
                log.Trace($"ExifRotateBitmap exception {e.ToLongString()}");
                return sampled;
            }
        }

        private Bitmap LoadResampledBitmap(ContentResolver contentResolver, Uri uri, int sampleSize)
        {
            using (var inputStream = contentResolver.OpenInputStream(uri))
            {
                var optionsDecode = new BitmapFactory.Options { InSampleSize = sampleSize };

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

            int rotationInDegrees;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                using (var stream = contentResolver.OpenInputStream(uri))
                {
                    //API 24+
                    using (var exif = new ExifInterface(stream))
                    {
                        var rotation = exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);
                        rotationInDegrees = ExifToDegrees(rotation);
                    }
                }
            }
            else
            {
                var realPath = GetRealPathFromUri(contentResolver, uri);
                if (realPath != null)
                {
                    var exif = new ExifInterface(realPath);
                    var rotation = exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);
                    rotationInDegrees = ExifToDegrees(rotation);
                }
                else
                {
                    rotationInDegrees = 0;
                    log.Trace("ExifRotateBitmap can not load exif data from external image on api < 24");
                }
            }

            if (rotationInDegrees == 0)
                return bitmap;
            using (var matrix = new Matrix())
            {
                matrix.PreRotate(rotationInDegrees);
                var newBitmap = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true);
                bitmap.Dispose();
                return newBitmap;
            }
        }

        private string GetRealPathFromUri(ContentResolver contentResolver, Uri uri)
        {
            var proj = new[] { MediaStore.Images.ImageColumns.Data };
            using (var cursor = contentResolver.Query(uri, proj, null, null, null))
            {
                var columnIndex = cursor.GetColumnIndexOrThrow(MediaStore.Images.ImageColumns.Data);
                if (cursor.MoveToFirst())
                    return cursor.GetString(columnIndex);
            }
            return null;
        }

        private static int ExifToDegrees(int exifOrientation)
        {
            switch (exifOrientation)
            {
                case (int)Orientation.Rotate90:
                    return 90;
                case (int)Orientation.Rotate180:
                    return 180;
                case (int)Orientation.Rotate270:
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
