using System;
using System.Collections.Generic;
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
using Uri = Android.Net.Uri;
using Android.Support.V4.Content;
using Android.Util;
using MvvmCross;
using MvvmCross.Exceptions;
using MvvmCross.Logging;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Views.Base;
using Stream = System.IO.Stream;

namespace Vapolia.Mvvmcross.PicturePicker.Droid
{
    public enum PicturePickerIntentRequestCode
    {
        PickFromFile = 22701,
        PickFromCamera,
        PickFromMultiFiles
    }

    [Android.Runtime.Preserve(AllMembers = true)]
    public class PicturePicker : MvxAndroidTask, IPicturePicker, IMultiPicturePicker
    {
        private readonly IMvxLog log;
        private readonly IMvxAndroidGlobals androidGlobals;
        private Uri photoUri;
        private RequestParameters currentRequestParameters;
        private bool shouldSaveToGallery;

        public PicturePicker(IMvxLog logger, IMvxAndroidGlobals androidGlobals)
        {
            log = logger;
            this.androidGlobals = androidGlobals;
            if(androidGlobals == null)
                throw new ArgumentNullException(nameof(androidGlobals));
            if(androidGlobals.ApplicationContext == null)
                throw new Exception("androidGlobals.ApplicationContext is null");
            if(androidGlobals.ApplicationContext.Resources == null)
                throw new Exception("androidGlobals.ApplicationContext.Resources is null");
            if(androidGlobals.ApplicationContext.PackageManager == null)
                throw new Exception("androidGlobals.ApplicationContext.PackageManager is null");
            if(androidGlobals.ApplicationContext.PackageName == null)
                throw new Exception("androidGlobals.ApplicationContext.PackageName is null");
        }

        public async Task<bool> ChoosePictureFromLibrary(string filePath, Action<Task<bool>> saving = null, int maxPixelWidth = 0, int maxPixelHeight = 0, int percentQuality = 80)
        {
            try
            {
                shouldSaveToGallery = false;
                var intent = new Intent(Intent.ActionGetContent);
                intent.SetType("image/*");
                var tcs = new TaskCompletionSource<bool>();

                async Task PictureAvailable(Stream stream)
                {
                    saving?.Invoke(Task.FromResult(true));
                    await SaveImage(filePath, stream, CancellationToken.None);
                    stream.Dispose();
                    saving?.Invoke(Task.FromResult(false));
                    tcs.SetResult(true);
                }

                ChoosePictureCommon(PicturePickerIntentRequestCode.PickFromFile, intent, maxPixelWidth, maxPixelHeight, percentQuality, PictureAvailable, () => tcs.SetResult(false));
                return await tcs.Task;
            }
            finally
            {
                currentRequestParameters = null;
            }
        }

        /// <summary>
        /// Multi pictures picker
        /// </summary>
        public async Task<List<string>> ChoosePicture(string storageFolder, MultiPicturePickerOptions options = null)
        {
            if(options == null)
                options = new MultiPicturePickerOptions();

            var files = new List<string>();
            CancellationTokenSource saving = null;
            shouldSaveToGallery = false;

            try
            {
                var intent = new Intent(Intent.ActionGetContent).PutExtra(Intent.ExtraAllowMultiple, true);
                intent.SetType("image/*");
                var tcs = new TaskCompletionSource<List<string>>();

                async Task NewPictureAvailable(Stream stream)
                {
                    try
                    {
                        if (saving == null)
                        {
                            saving = new CancellationTokenSource();
                            options.SavingAction?.Invoke(saving.Token);
                        }

                        var filePath = System.IO.Path.Combine(storageFolder, Guid.NewGuid().ToString("N") + ".jpg");
                        await SaveImage(filePath, stream, CancellationToken.None);
                        stream.Dispose();
                        files.Add(filePath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                void Finished()
                {
                    saving?.Cancel();
                    tcs.TrySetResult(files);
                }

                ChoosePictureCommon(PicturePickerIntentRequestCode.PickFromMultiFiles, intent, options.MaxPixelWidth, options.MaxPixelHeight, options.PercentQuality, NewPictureAvailable, Finished);
                return await tcs.Task;
            }
            finally
            {
                currentRequestParameters = null;
            }
        }

        public bool HasCamera => Application.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);

        public async Task<bool> TakePicture(string filePath, Action<Task<bool>> saving = null, int maxPixelWidth = 0, int maxPixelHeight = 0, int percentQuality = 80, bool useFrontCamera = false, bool saveToGallery = false)
        {
            try
            {
                shouldSaveToGallery = saveToGallery;
                var intent = new Intent(MediaStore.ActionImageCapture);

                var file = Java.IO.File.CreateTempFile(Guid.NewGuid().ToString("N"), ".jpg", androidGlobals.ApplicationContext.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures));
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    var res = androidGlobals.ApplicationContext.Resources;
                    var stringId = res.GetIdentifier("vapolia_picturepicker_fileprovidername", "string", androidGlobals.ApplicationContext.PackageName);
                    if(stringId <= 0)
                        throw new Exception("you must declare a string value in your app's resource: @string/vapolia_picturepicker_fileprovidername with value 'com.yourcompany.yourapp.vapolia.picturepicker'. ex: <resources>\r\n    <string name=\"vapolia_picturepicker_fileprovidername\">com.yourcompany.yourapp.vapolia.picturepicker</string>\r\n</resources>\r\n");
                    var fileProviderName = res.GetString(stringId); //vapolia.mvvmcross.picturepicker.fileProvider
                    photoUri = FileProvider.GetUriForFile(androidGlobals.ApplicationContext, fileProviderName, file); //Android 22.1+, required on android 24+
                    if(photoUri == null)
                        throw new Exception("FileProvider.GetUriForFile returned null!");
                }
                else
                    photoUri = Uri.FromFile(file);
                //cachedUriLocation = GetNewImageUri();
                intent.PutExtra(MediaStore.ExtraOutput, photoUri);
                intent.PutExtra("outputFormat", Bitmap.CompressFormat.Jpeg.ToString());
                intent.PutExtra("return-data", true);
                if (useFrontCamera && Application.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFront))
                {
                    intent.PutExtra("android.intent.extras.CAMERA_FACING", (int) Android.Hardware.CameraFacing.Front); //lower than LOLLIPOP_MR1
                    intent.PutExtra("android.intent.extras.LENS_FACING_FRONT", 1); //LOLLIPOP_MR1 or greater, except Android7
                    intent.PutExtra("android.intent.extra.USE_FRONT_CAMERA", true); //Android7
                }

                var tcs = new TaskCompletionSource<bool>();

                async Task PictureAvailable(Stream stream)
                {
                    await SaveImage(filePath, stream, CancellationToken.None);
                    tcs.SetResult(true);
                }

                void AssumeCancelled() => tcs.SetResult(false);

                ChoosePictureCommon(PicturePickerIntentRequestCode.PickFromCamera, intent, maxPixelWidth, maxPixelHeight, percentQuality, PictureAvailable, AssumeCancelled);

                return await tcs.Task;
            }
            catch (Exception e)
            {
                log.Error(e, "Error in TakePicture");
                currentRequestParameters.AssumeCancelled();
            }
            finally
            {
                currentRequestParameters = null;
            }

            return false;
        }

        private async Task SaveImage(string filePath, Stream stream, CancellationToken cancel)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var fileStream = File.OpenWrite(filePath))
                await stream.CopyToAsync(fileStream, 81920, cancel);
        }

        //private Uri GetNewImageUri()
        //{
        //    // Optional - specify some metadata for the picture
        //    var contentValues = new ContentValues();
        //    //contentValues.Put(MediaStore.Images.ImageColumnsConsts.Description, "A camera photo");

        //    // Specify where to put the image
        //    return androidGlobals.ApplicationContext.ContentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, contentValues);
        //}

        public void ChoosePictureCommon(PicturePickerIntentRequestCode pickId, Intent intent, int maxPixelWidth, int maxPixelHeight,
                                        int percentQuality, Func<Stream,Task> pictureAvailable, Action assumeCancelled)
        {
            if (currentRequestParameters != null)
                throw new MvxException("Cannot request a second picture while the first request is still pending");

            currentRequestParameters = new RequestParameters(maxPixelWidth, maxPixelHeight, percentQuality, pictureAvailable, assumeCancelled);
            if (intent.ResolveActivity(androidGlobals.ApplicationContext.PackageManager) != null)
            {
                var topActivity = Mvx.IoCProvider.Resolve<IMvxAndroidCurrentTopActivity>().Activity;
                if(topActivity == null)
                    throw new MvxException("top activity is null");
                StartActivityForResult((int) pickId, intent);
            }
            else
                assumeCancelled();
        }

        protected override void ProcessMvxIntentResult(MvxIntentResultEventArgs result)
        {
            //thumbnail !
            //var thumbnail = (Bitmap)result.Data.Extras.Get("data"); 

            Uri uri = null;
            switch ((PicturePickerIntentRequestCode)result.RequestCode)
            {
                case PicturePickerIntentRequestCode.PickFromFile:
                    if(result.ResultCode == Result.Ok)
                        uri = result.Data?.Data;
                    break;
                case PicturePickerIntentRequestCode.PickFromCamera:
                    if(result.ResultCode == Result.Ok)
                        uri = photoUri;
                    break;
                case PicturePickerIntentRequestCode.PickFromMultiFiles:
                    if (result.ResultCode == Result.Ok && (result.Data?.ClipData?.ItemCount > 0 || result.Data?.Data != null))
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                if (result.Data.ClipData != null)
                                {
                                    var n = result.Data.ClipData.ItemCount;
                                    for (var i = 0; i < n; i++)
                                    {
                                        uri = result.Data.ClipData.GetItemAt(i).Uri;
                                        if (!(await ProcessPictureUri(result, uri)))
                                            break;
                                    }
                                }
                                else
                                {
                                    uri = result.Data.Data;
                                    await ProcessPictureUri(result, uri);
                                }

                                currentRequestParameters.AssumeCancelled();
                                currentRequestParameters = null;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        });
                    }
                    else
                    {
                        currentRequestParameters.AssumeCancelled();
                        currentRequestParameters = null;
                    }
                    return;
                default:
                    // ignore this result - it's not for us
                    log.Error($"Unexpected request received from MvxIntentResult - request was {result.RequestCode}");
                    return;
            }

            if (uri != null)
            {
                //Do heavy work in a worker thread
                Task.Run(async () =>
                {
                    if (!(await ProcessPictureUri(result, uri)))
                        currentRequestParameters.AssumeCancelled();
                    currentRequestParameters = null;
                });
            }
            else
            {
                currentRequestParameters.AssumeCancelled();
                currentRequestParameters = null;
            }
        }

        private async Task<bool> ProcessPictureUri(MvxIntentResultEventArgs result, Uri uri)
        {
            if (string.IsNullOrEmpty(uri.Path))
            {
                log.Error("Empty uri or file path received for MvxIntentResult");
                return false;
            }

            log.Trace("Loading InMemoryBitmap started...");
            var memoryStream = LoadInMemoryBitmap(uri);
            if (memoryStream == null)
            {
                log.Trace("Loading InMemoryBitmap failed...");
                return false;
            }
            log.Trace("Loading InMemoryBitmap complete...");
            log.Trace("Sending pictureAvailable...");
            await currentRequestParameters.PictureAvailable(memoryStream).ConfigureAwait(false);
            log.Trace("pictureAvailable completed...");
            return true;
        }

        private MemoryStream LoadInMemoryBitmap(Uri uri)
        {
            var memoryStream = new MemoryStream();
            var bitmap = LoadScaledBitmap(uri);
            if (bitmap == null)
                return null;

            if (shouldSaveToGallery)
            {
                MediaStore.Images.Media.InsertImage(androidGlobals.ApplicationContext.ContentResolver, bitmap, $"{DateTime.Now.ToString("O").Replace(':','-').Replace('.','-').Replace('T',' ')}", "");
            }

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
            var maxSize = GetMaximumDimension(contentResolver, uri);

            Bitmap sampled;
            if (currentRequestParameters.MaxPixelWidth != 0 || currentRequestParameters.MaxPixelHeight != 0)
            {
                int sampleSize=0; // = Math.Max(currentRequestParameters.MaxPixelWidth, currentRequestParameters.MaxPixelHeight);
                if (currentRequestParameters.MaxPixelWidth != 0)
                    sampleSize = (int)Math.Ceiling(maxSize.Width / (double)currentRequestParameters.MaxPixelWidth);
                if (currentRequestParameters.MaxPixelHeight != 0)
                    sampleSize = Math.Max(sampleSize, (int)Math.Ceiling(maxSize.Height / (double)currentRequestParameters.MaxPixelHeight));

                if (sampleSize < 1)
                    sampleSize = 1;
                sampled = LoadResampledBitmap(contentResolver, uri, sampleSize);
            }
            else
                sampled = LoadResampledBitmap(contentResolver, uri, 1);

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
            if (sampleSize == 1)
                return MediaStore.Images.Media.GetBitmap(contentResolver, uri);

            using (var inputStream = contentResolver.OpenInputStream(uri))
            {
                var optionsDecode = new BitmapFactory.Options { InSampleSize = sampleSize };

                return BitmapFactory.DecodeStream(inputStream, null, optionsDecode);
            }
        }

        private static Size GetMaximumDimension(ContentResolver contentResolver, Uri uri)
        {
            using (var inputStream = contentResolver.OpenInputStream(uri))
            {
                var optionsJustBounds = new BitmapFactory.Options
                {
                    InJustDecodeBounds = true
                };
                // ReSharper disable once UnusedVariable
                var metadataResult = BitmapFactory.DecodeStream(inputStream, null, optionsJustBounds);
                return new Size(optionsJustBounds.OutWidth, optionsJustBounds.OutHeight);
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
            switch (uri.Scheme)
            {
                    case "file":
                        return uri.Path;

                    case "content":
                        var proj = new[] { MediaStore.Images.ImageColumns.Data };
                        using (var cursor = contentResolver.Query(uri, proj, null, null, null))
                        {
                            var columnIndex = cursor.GetColumnIndexOrThrow(MediaStore.Images.ImageColumns.Data);
                            if (cursor.MoveToFirst())
                                return cursor.GetString(columnIndex);
                        }
                        log.Error($"PicturePicker content uri not found: {uri}");
                        break;

                    default:
                        log.Error($"PicturePicker uri not supported: {uri}");
                        throw new NotSupportedException($"uri not supported {uri}");
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
            public RequestParameters(int maxPixelWidth, int maxPixelHeight, int percentQuality, Func<Stream,Task> pictureAvailable, Action assumeCancelled)
            {
                MaxPixelWidth = maxPixelWidth;
                MaxPixelHeight = maxPixelHeight;
                PercentQuality = percentQuality;
                AssumeCancelled = assumeCancelled;
                PictureAvailable = pictureAvailable;
            }

            public Func<Stream,Task> PictureAvailable { get; }
            public Action AssumeCancelled { get; }
            public int MaxPixelWidth { get; }
            public int MaxPixelHeight { get; }
            public int PercentQuality { get; }
        }

        #endregion
    }
}
