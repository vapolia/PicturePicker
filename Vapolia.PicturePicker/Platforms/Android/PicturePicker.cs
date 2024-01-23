using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Uri = Android.Net.Uri;
using Android.Util;
using Microsoft.Extensions.Logging;
using Application = Android.App.Application;
using File = System.IO.File;
using Path = System.IO.Path;
using Size = Android.Util.Size;
using Stream = System.IO.Stream;

namespace Vapolia.PicturePicker.PlatformLib;

public enum PicturePickerIntentRequestCode
{
    PickFromFile = 22701,
    PickFromCamera,
    PickFromMultiFiles
}

[Android.Runtime.Preserve(AllMembers = true)]
public class PicturePicker : IPicturePicker, IMultiPicturePicker
{
    private readonly ILogger log;
    private readonly Context applicationContext;
    private bool shouldSaveToGallery;

    public PicturePicker(ILogger logger)
    {
        log = logger;
        applicationContext = global::Android.App.Application.Context;
    }

    public async Task<bool> ChoosePictureFromLibrary(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth = 0, int maxPixelHeight = 0, int percentQuality = 80)
    {
        shouldSaveToGallery = false;
        // var intent = new Intent(Intent.ActionGetContent)
        //             .SetType("image/*");
        //             //.AddFlags(ActivityFlags.GrantReadUriPermission);
        //             //.AddFlags(ActivityFlags.GrantPersistableUriPermission);

        var intent = new Intent(Intent.ActionOpenDocument) //,MediaStore.Images.Media.ExternalContentUri
            .AddCategory(Intent.CategoryOpenable)
            .SetType("image/*")
            .PutExtra(Intent.ExtraAllowMultiple, false)
            .AddFlags(ActivityFlags.GrantReadUriPermission);

                        
        var tcs = new TaskCompletionSource<bool>();

        async Task PictureAvailable(Stream stream)
        {
            saving?.Invoke(Task.FromResult(true));
            await SaveImage(filePath, stream, CancellationToken.None);
            await stream.DisposeAsync();
            saving?.Invoke(Task.FromResult(false));
            tcs.SetResult(true);
        }

        var ok = await ChoosePictureCommon(PicturePickerIntentRequestCode.PickFromFile, intent, maxPixelWidth, maxPixelHeight, percentQuality, PictureAvailable);
        if (!ok)
            tcs.TrySetResult(false);

        return await tcs.Task;
    }

    /// <summary>
    /// Multi pictures picker
    /// </summary>
    public async Task<List<string>> ChoosePicture(string storageFolder, MultiPicturePickerOptions options = default, CancellationToken cancel = default)
    {
        var files = new List<string>();
        CancellationTokenSource? saving = null;
        shouldSaveToGallery = false;

        var intent = new Intent(Intent.ActionOpenDocument) //,MediaStore.Images.Media.ExternalContentUri
            .AddCategory(Intent.CategoryOpenable)
            .SetType("image/*")
            .PutExtra(Intent.ExtraAllowMultiple, true)
            .AddFlags(ActivityFlags.GrantReadUriPermission);

        async Task NewPictureAvailable(Stream stream)
        {
            try
            {
                saving ??= new CancellationTokenSource();
                options.SavingAction?.Invoke(saving.Token);

                var filePath = System.IO.Path.Combine(storageFolder, Guid.NewGuid().ToString("N") + ".jpg");
                await SaveImage(filePath, stream, cancel);
                await stream.DisposeAsync();
                files.Add(filePath);
            }
            catch (Exception e)
            {
                log.LogError(e, "Error saving image");
            }
        }

        await ChoosePictureCommon(PicturePickerIntentRequestCode.PickFromMultiFiles, intent, options.MaxPixelWidth, options.MaxPixelHeight, options.PercentQuality, NewPictureAvailable);
        saving?.Cancel();
        return files; 
    }

    public bool HasCamera => Application.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Saving to the gallery requires this line in the final app for API <= 28:
    /// [assembly:UsesPermission(Android.Manifest.Permission.WriteExternalStorage, MaxSdkVersion = 28)]
    /// </remarks>
    public async Task<bool> TakePicture(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth = 0, int maxPixelHeight = 0, int percentQuality = 80, bool useFrontCamera = false, bool saveToGallery = false, CancellationToken cancel = default)
    {
        shouldSaveToGallery = saveToGallery;

        var pathForFileProvider = Java.IO.File.CreateTempFile(Guid.NewGuid().ToString("N"), ".jpg", FileProviderHelper.GetTemporaryDirectory()).AbsolutePath;

        var intent = new Intent(MediaStore.ActionImageCapture)
            //.PutExtra(MediaStore.ExtraOutput, photoUri) //Set by IntermediateActivity
            .PutExtra("outputFormat", Bitmap.CompressFormat.Jpeg.ToString())
            .PutExtra("return-data", true);
        if (useFrontCamera && Application.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFront))
        {
            intent.PutExtra("android.intent.extras.CAMERA_FACING", (int) Android.Hardware.CameraFacing.Front) //lower than LOLLIPOP_MR1
                .PutExtra("android.intent.extras.LENS_FACING_FRONT", 1) //LOLLIPOP_MR1 or greater, except Android7
                .PutExtra("android.intent.extra.USE_FRONT_CAMERA", true); //Android7
        }

        var tcs = new TaskCompletionSource<bool>();

        async Task PictureAvailable(Stream stream)
        {
            try
            {
                if(stream == Stream.Null)
                    File.Move(pathForFileProvider, filePath);
                else
                {
                    await using var fs = File.Create(filePath);
                    await stream.CopyToAsync(fs, cancel).ConfigureAwait(false);
                    if(File.Exists(pathForFileProvider))
                        File.Delete(pathForFileProvider);
                }

                if (shouldSaveToGallery)
                {
                    await SaveImageToStorage(filePath, cancel);
                    ////TODO: Obsolete in 29+
                    //MediaStore.Images.Media.InsertImage(applicationContext.ContentResolver, pathForFileProvider, System.IO.Path.GetFileNameWithoutExtension(filePath), "");

                    //if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                    //{
                    //    //TODO: cf https://stackoverflow.com/questions/57030990/how-to-save-an-image-in-a-subdirectory-on-android-q-whilst-remaining-backwards-c
                    //    //var values = new ContentValues();
                    //}
                }

                tcs.TrySetResult(true);
            }
            catch (Exception e)
            {
                log.LogError(e, "Can't save image");
                tcs.TrySetResult(false);
            }
        }

        void AssumeCancelled() => tcs.TrySetResult(false);

        var ok = await ChoosePictureCommon(PicturePickerIntentRequestCode.PickFromCamera, intent, maxPixelWidth, maxPixelHeight, percentQuality, PictureAvailable, pathForFileProvider);
        if (!ok)
            AssumeCancelled();

        return await tcs.Task;
    }

    private async Task<bool> SaveImageToStorage(string filePath, CancellationToken cancel = default)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            var values = new ContentValues();
            values.Put(MediaStore.Images.Media.InterfaceConsts.DisplayName, System.IO.Path.GetFileNameWithoutExtension(filePath));
            values.Put(MediaStore.Images.Media.InterfaceConsts.MimeType, "image/jpeg");
            values.Put(MediaStore.Images.Media.InterfaceConsts.RelativePath, Android.OS.Environment.DirectoryPictures);
            var uri = applicationContext.ContentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, values);
                
            var imageOutStream = applicationContext.ContentResolver.OpenOutputStream(uri);
            //await bitmap.CompressAsync(Bitmap.CompressFormat.Jpeg, percentQuality, imageOutStream);
            await using var fileStream = File.OpenRead(filePath);
            await fileStream.CopyToAsync(imageOutStream, 81920, cancel);
            imageOutStream.Close();

            return true;
        }
        else
        {
            //var imagesDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).AbsolutePath;
            //var targetFilePath = Path.Combine(imagesDir, applicationContext.PackageName, Path.GetFileName(filePath));
            //Directory.CreateDirectory(targetFilePath);
            //File.Copy(filePath, targetFilePath);

            //permission Denial: writing com.android.providers.media.MediaProvider uri content://media/external/images/media from pid=19063, uid=10089 requires android.permission.WRITE_EXTERNAL_STORAGE, or grantUriPermission()
            var ok = PermissionStatus.Unknown;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ok = await Permissions.RequestAsync<Permissions.StorageWrite>();
            });

            if (ok == PermissionStatus.Granted)
            {
                var path = MediaStore.Images.Media.InsertImage(applicationContext.ContentResolver, filePath, Path.GetFileNameWithoutExtension(filePath), "");
                return path != null;
            }

            return false;
        }
    }

    private async Task SaveImage(string filePath, Stream stream, CancellationToken cancel)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        await using var fileStream = File.OpenWrite(filePath);
        await stream.CopyToAsync(fileStream, 81920, cancel);
    }

    //private Uri GetNewImageUri()
    //{
    //    // Optional - specify some metadata for the picture
    //    var contentValues = new ContentValues();
    //    //contentValues.Put(MediaStore.Images.ImageColumnsConsts.Description, "A camera photo");

    //    // Specify where to put the image
    //    return applicationContext.ContentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, contentValues);
    //}

    private async Task<bool> ChoosePictureCommon(PicturePickerIntentRequestCode pickId, Intent intent, int maxPixelWidth, int maxPixelHeight,
        int percentQuality, Func<Stream,Task> pictureAvailable, string? extraOutputPath = null)
    {
        var tcs = new TaskCompletionSource<bool>();     
        void AssumeCancelled() => tcs.TrySetResult(false);

        async Task PictureAvailable(Stream stream)
        {
            try
            {
                await pictureAvailable(stream);
                tcs.TrySetResult(true);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        }

        var currentRequestParameters = new RequestParameters(maxPixelWidth, maxPixelHeight, percentQuality, PictureAvailable);
        _ = IntermediateActivity.StartAsync(intent, (int) pickId, extraOutputPath).ContinueWith(t =>
        {
            if (t.IsCanceled)
                AssumeCancelled();
            else
            {
                ProcessIntentResult(t.Result, pickId, currentRequestParameters, AssumeCancelled, extraOutputPath); 
            }
        });

        return await tcs.Task;
    }

    private void ProcessIntentResult(Intent? data, PicturePickerIntentRequestCode pickId, RequestParameters currentRequestParameters, Action assumeCancelled, string? extraOutputPath)
    {
        //thumbnail !
        //var thumbnail = (Bitmap)result.Data.Extras.Get("data"); 

        Uri? uri;
        switch (pickId)
        {
            case PicturePickerIntentRequestCode.PickFromFile:
                uri = data?.Data;
                break;
            case PicturePickerIntentRequestCode.PickFromCamera:
                //data?.Data = bitmap thumbnail
                //uri = photoUri;
                uri = extraOutputPath != null ? Uri.FromFile(new Java.IO.File(extraOutputPath)) : Uri.Empty;
                break;
            case PicturePickerIntentRequestCode.PickFromMultiFiles:
                if (data?.ClipData?.ItemCount > 0 || data?.Data != null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            if (data.ClipData != null)
                            {
                                var n = data.ClipData.ItemCount;
                                for (var i = 0; i < n; i++)
                                {
                                    uri = data.ClipData.GetItemAt(i)!.Uri!;
                                    if (!(await ProcessPictureUri(uri, currentRequestParameters)))
                                        break;
                                }
                            }
                            else
                            {
                                uri = data.Data!;
                                await ProcessPictureUri(uri, currentRequestParameters);
                            }
                        }
                        catch (Exception e)
                        {
                            log.LogError(e, "Error ProcessPictureUri");
                        }

                        assumeCancelled();
                    });
                }
                else
                {
                    assumeCancelled();
                }
                return;
            default:
                throw new Exception($"Unexpected request received from MvxIntentResult - request was {pickId}");
        }

        if (uri == Uri.Empty)
            currentRequestParameters.PictureAvailable(Stream.Null);
        else if (uri != null)
        {
            //Do heavy work in a worker thread
            Task.Run(async () =>
            {
                if (!(await ProcessPictureUri(uri, currentRequestParameters)))
                    assumeCancelled();
            });
        }
        else
        {
            assumeCancelled();
        }
    }

    private async Task<bool> ProcessPictureUri(Uri uri, RequestParameters currentRequestParameters)
    {
        if (string.IsNullOrEmpty(uri.Path))
        {
            log.LogError("Empty uri or file path received for MvxIntentResult");
            return false;
        }

        log.LogTrace("Loading InMemoryBitmap started...");
        var memoryStream = await LoadInMemoryBitmap(uri, currentRequestParameters);
        if (memoryStream == null)
        {
            log.LogTrace("Loading InMemoryBitmap failed...");
            return false;
        }
        log.LogTrace("Loading InMemoryBitmap complete...");
        log.LogTrace("Sending pictureAvailable...");
        await currentRequestParameters.PictureAvailable(memoryStream).ConfigureAwait(false);
        log.LogTrace("pictureAvailable completed...");
        return true;
    }

    private async Task<MemoryStream?> LoadInMemoryBitmap(Uri uri, RequestParameters currentRequestParameters)
    {
        var memoryStream = new MemoryStream();
        var bitmap = LoadScaledBitmap();
        if (bitmap == null)
            return null;

        if (shouldSaveToGallery)
        {
            MediaStore.Images.Media.InsertImage(applicationContext.ContentResolver, bitmap, $"{DateTime.Now.ToString("O").Replace(':', '-').Replace('.', '-').Replace('T', ' ')}", "");
        }

        var quality = currentRequestParameters.PercentQuality == 0 ? 80 : currentRequestParameters.PercentQuality;
        using (bitmap)
            await bitmap.CompressAsync(Bitmap.CompressFormat.Jpeg, quality, memoryStream);

        memoryStream.Seek(0L, SeekOrigin.Begin);
        return memoryStream;


        Bitmap? LoadScaledBitmap()
        {
            using var contentResolver = applicationContext.ContentResolver;
            if (contentResolver == null)
                return null;

            var (sampled,exif) = LoadResampledBitmap(contentResolver, uri, currentRequestParameters.MaxPixelWidth, currentRequestParameters.MaxPixelHeight);

            try
            {
                return ExifRotateBitmap(sampled, exif);
            }
            catch (Exception e)
            {
                log.LogTrace(e, $"ExifRotateBitmap exception");
                return sampled;
            }
            finally
            {
                exif?.Dispose();
            }
        }
    }

    private (Bitmap?,ExifInterface?) LoadResampledBitmap(ContentResolver contentResolver, Uri uri, int destWidth, int destHeight)
    {
        static Size GetMaximumDimension(Stream inputStream)
        {
            var optionsJustBounds = new BitmapFactory.Options { InJustDecodeBounds = true };
            // ReSharper disable once UnusedVariable
            var metadataResult = BitmapFactory.DecodeStream(inputStream, null, optionsJustBounds);
            return new Size(optionsJustBounds.OutWidth, optionsJustBounds.OutHeight);
        }
            
        static int GetBestSampleSize(Size sourceSize, int destWidth, int destHeight)
        {
            var sampleSize = 1; // = Math.Max(currentRequestParameters.MaxPixelWidth, currentRequestParameters.MaxPixelHeight);
            if (destWidth != 0)
                sampleSize = (int) Math.Ceiling(sourceSize.Width / (double) destWidth);
            if (destHeight != 0)
                sampleSize = Math.Max(sampleSize, (int) Math.Ceiling(sourceSize.Height / (double) destHeight));
            
            if (sampleSize < 1)
                sampleSize = 1;
            return sampleSize;
        }
            
        using var inputStream = contentResolver.OpenInputStream(uri);
        if (inputStream == null)
            return (null,null);
        var maxSize = GetMaximumDimension(inputStream);
        var sampleSize = GetBestSampleSize(maxSize, destWidth, destHeight);
            
        inputStream.Seek(0, SeekOrigin.Begin);
        ExifInterface? exif = null;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
        {
            //API 24+
            exif = new ExifInterface(inputStream);
        }
        else
        {
            var realPath = GetRealPathFromUri(contentResolver, uri);
            if (realPath != null)
                exif = new ExifInterface(realPath);
            else
                log.LogTrace("Can not load exif data from external image on api < 24");
        }
            
        //This returns a straight bitmap. But <e want a rotated bitmap as we straight it ourself. So don't use this code.
//             if (sampleSize == 1)
//             {
//                 if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
//                 {
//                     var source = ImageDecoder.CreateSource(contentResolver, uri);
//                     return ImageDecoder.DecodeBitmap(source);
//                 }
//
// #pragma warning disable 618
//                 //Obsolete in API 28+ (crash)
//                 return MediaStore.Images.Media.GetBitmap(contentResolver, uri);
// #pragma warning restore 618
//             }

        var optionsDecode = new BitmapFactory.Options
        {
            InSampleSize = sampleSize,
            InPreferredConfig = Bitmap.Config.Argb8888
        };

        inputStream.Seek(0, SeekOrigin.Begin);
        return (BitmapFactory.DecodeStream(inputStream, null, optionsDecode), exif);
    }


    private Bitmap? ExifRotateBitmap(Bitmap? bitmap, ExifInterface? exif)
    {
        if (bitmap == null)
            return null;

        var rotation = exif?.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal) ?? 0;
        var rotationInDegrees = ExifToDegrees(rotation);

        if (rotationInDegrees == 0)
            return bitmap;
        using var matrix = new Matrix();
        matrix.PreRotate(rotationInDegrees);

        var w = bitmap.Width;
        var h = bitmap.Height;
            
        var newBitmap = Bitmap.CreateBitmap(bitmap, 0, 0, w, h, matrix, true);
        bitmap.Dispose();
        return newBitmap;
    }

    /// <summary>
    /// MediaStore.Images.Media.EXTERNAL_CONTENT_URI
    /// MediaStore.Images.Media._ID
    /// </summary>
    private string? GetRealPathFromUri(ContentResolver contentResolver, Uri uri)
    {
        switch (uri.Scheme)
        {
            case "file":
                return uri.Path;

            case "content":
                var proj = new[] { MediaStore.Images.ImageColumns.Data };
                using (var cursor = contentResolver.Query(uri, proj, null, null, null))
                {
                    if (cursor != null)
                    {
                        var columnIndex = cursor.GetColumnIndexOrThrow(MediaStore.Images.ImageColumns.Data);
                        if (cursor.MoveToFirst())
                            return cursor.GetString(columnIndex);
                    }
                }
                log.LogError($"PicturePicker content uri not found: {uri}");
                break;

            default:
                log.LogError($"PicturePicker uri not supported: {uri}");
                throw new NotSupportedException($"uri not supported {uri}");
        }

        return null;
    }

    private static int ExifToDegrees(int exifOrientation)
    {
        return exifOrientation switch
        {
            (int) Orientation.Rotate90 => 90,
            (int) Orientation.Rotate180 => 180,
            (int) Orientation.Rotate270 => 270,
            _ => 0
        };
    }

    #region Nested type: RequestParameters

    private class RequestParameters
    {
        public RequestParameters(int maxPixelWidth, int maxPixelHeight, int percentQuality, Func<Stream,Task> pictureAvailable)
        {
            MaxPixelWidth = maxPixelWidth;
            MaxPixelHeight = maxPixelHeight;
            PercentQuality = percentQuality;
            PictureAvailable = pictureAvailable;
        }

        public Func<Stream,Task> PictureAvailable { get; }
        public int MaxPixelWidth { get; }
        public int MaxPixelHeight { get; }
        public int PercentQuality { get; }
    }

    #endregion
}