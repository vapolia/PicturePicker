using System;
using System.IO;
using CoreGraphics;
using System.Threading.Tasks;
using Foundation;
using ImageIO;
using MobileCoreServices;
using MvvmCross.Exceptions;
using MvvmCross.Logging;
using MvvmCross.Platforms.Ios.Views;
using Photos;
using UIKit;

namespace Vapolia.Mvvmcross.PicturePicker.Touch
{
    [Preserve(AllMembers = true)]
    public sealed class PicturePicker : IPicturePicker, IDisposable
    {
        private readonly IMvxLog log;
        private readonly UIImagePickerController picker;
        // ReSharper disable InconsistentNaming
        private int _maxPixelWidth, _maxPixelHeight;
        private int _percentQuality;
        private string _filePath;
        // ReSharper restore InconsistentNaming
        private Action<Task<bool>> savingTaskAction;
        private TaskCompletionSource<bool> tcs;
        private bool shouldSaveToGallery;

        public PicturePicker(IMvxLog logger)
        {
            log = logger;

            picker = new UIImagePickerController();

            picker.FinishedPickingMedia += async (sender, args) =>
            {
                var image = args.EditedImage ?? args.OriginalImage;
                var metadata = args.MediaMetadata;

                if (metadata == null)
                {
                    //Requiert l'acces à la photo lib
                    //var asset = args.PHAsset; //ios11+
                    var asset = (PHAsset)PHAsset.FetchAssets( new [] { args.ReferenceUrl ?? args.ImageUrl ?? args.MediaUrl }, new PHFetchOptions() { IncludeAssetSourceTypes = PHAssetSourceType.UserLibrary|PHAssetSourceType.iTunesSynced|PHAssetSourceType.CloudShared }).firstObject; //ios8-10

                    if (asset != null)
                    {
                        var tcs0 = new TaskCompletionSource<NSDictionary>();
                        PHImageManager.DefaultManager.RequestImageData(asset, new PHImageRequestOptions {NetworkAccessAllowed = false, DeliveryMode = PHImageRequestOptionsDeliveryMode.Opportunistic, ResizeMode = PHImageRequestOptionsResizeMode.None}, (nsData, uti, orientation, dictionary) =>
                        {
                            var imageSource = CGImageSource.FromData(nsData);
                            var meta = imageSource.CopyProperties(new CGImageOptions(), 0);
                            tcs0.TrySetResult(meta);
                        });
                        metadata = await tcs0.Task;
                    }
                }

#if DEBUG
                if (metadata != null)
                {
                    var dic = metadata.ToDic();
                    var j = 0;
                }
                else
                {
                    var data = image.AsJPEG((float)(_percentQuality / 100.0));
                    using (var stream = data.AsStream())
                    {
                        var info = new JpegInfoService(stream);
                        var lat = info.GpsLatitude;
                        var lng = info.GpsLongitude;
                    }
                }
#endif

                await HandleImagePick(image, metadata);
            };

            //Obsolete
            //picker.FinishedPickingImage += async (sender, args) =>
            //{
            //    await HandleImagePick(args.Image, null);
            //};

            picker.Canceled += async (sender, args) =>
            {
                await picker.DismissViewControllerAsync(true);
                //picker.Dispose();
                tcs.SetResult(false);
                tcs = null;
            };
        }

        public bool HasCamera => UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera)
                        || UIImagePickerController.IsCameraDeviceAvailable(UIImagePickerControllerCameraDevice.Front)
                        || UIImagePickerController.IsCameraDeviceAvailable(UIImagePickerControllerCameraDevice.Rear);

        public Task<bool> ChoosePictureFromLibrary(string filePath, Action<Task<bool>> saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality = 80)
        {
            savingTaskAction = saving;
            picker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
            picker.AllowsEditing = true;
            picker.AllowsImageEditing = true;
            shouldSaveToGallery = false;
            return ChoosePictureCommon(filePath, maxPixelWidth, maxPixelHeight, percentQuality);
        }

        public Task<bool> TakePicture(string filePath, Action<Task<bool>> saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality = 0, bool useFrontCamera=false, bool saveToGallery=false)
        {
            if (!HasCamera)
            {
                log.Warn("Source type Camera not available on this device.");
                return Task.FromResult(false);
            }

            var camera = useFrontCamera ? UIImagePickerControllerCameraDevice.Front : UIImagePickerControllerCameraDevice.Rear;
            if (!UIImagePickerController.IsCameraDeviceAvailable(camera))
            {
                camera = useFrontCamera ? UIImagePickerControllerCameraDevice.Rear : UIImagePickerControllerCameraDevice.Front;
                if (!UIImagePickerController.IsCameraDeviceAvailable(camera))
                {
                    log.Warn("No camera available on this device.");
                    return Task.FromResult(false);
                }
            }

            savingTaskAction = saving;
            shouldSaveToGallery = saveToGallery;
            picker.SourceType = UIImagePickerControllerSourceType.Camera;
            picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Photo;
            picker.CameraDevice = useFrontCamera ? UIImagePickerControllerCameraDevice.Front : UIImagePickerControllerCameraDevice.Rear;
            picker.AllowsEditing = false;
            picker.AllowsImageEditing = false;
            picker.Editing = false;
            return ChoosePictureCommon(filePath, maxPixelWidth, maxPixelHeight, percentQuality);
        }

        private Task<bool> ChoosePictureCommon(string filePath, int maxPixelWidth, int maxPixelHeight, int percentQuality)
        {
            if (tcs != null)
            {
                log.Error("PicturePicker: A call is already in progress");
                return Task.FromResult(false);
            }

            tcs = new TaskCompletionSource<bool>();

            _maxPixelWidth = maxPixelWidth;
            _maxPixelHeight = maxPixelHeight;
            _percentQuality = percentQuality;
            _filePath = filePath;

            var modalHost = UIApplication.SharedApplication.KeyWindow.GetTopModalHostViewController();
            modalHost.PresentViewController(picker, true, () => tcs.SetResult(false));
            return tcs.Task;
        }

        private async Task HandleImagePick(UIImage image, NSDictionary metadata)
        {
            string imageFile = null;
            if (image != null)
            {
                var tcsSaving = new TaskCompletionSource<bool>();
                savingTaskAction?.Invoke(tcsSaving.Task);

                if (shouldSaveToGallery)
                {
                    var tcs2 = new TaskCompletionSource<bool>();
                    image.SaveToPhotosAlbum((uiImage, error) => tcs2.TrySetResult(error == null));
                    await tcs2.Task;
                }

                try
                {
                    // resize the image
                    if (_maxPixelWidth > 0 || _maxPixelHeight > 0)
                        image = image.ImageToFitSize(new CGSize(_maxPixelWidth, _maxPixelHeight));

                    if (File.Exists(_filePath))
                        File.Delete(_filePath);

                    var imageDestination = CGImageDestination.Create(new CGDataConsumer(NSUrl.FromFilename(_filePath)), UTType.JPEG, 1, new CGImageDestinationOptions {LossyCompressionQuality = (float) (_percentQuality / 100.0)});
                    imageDestination.AddImage(image.CGImage, metadata);
                    if (!imageDestination.Close()) //Dispose is called by Close ...
                        log.Error("PicturePicker: failed to copy photo on save to {0}", _filePath);
                    else
                        imageFile = _filePath;

                    //using (var data = image.AsJPEG((float) (_percentQuality/100.0)))
                    //{
                    //    using (var stream = data.AsStream())
                    //    {
                    //        await ImageHelper.SaveImage(_filePath, stream, CancellationToken.None).ConfigureAwait(false);
                    //        imageFile = _filePath;
                    //    }
                    //}

                    tcsSaving.SetResult(true);
                }
                catch (Exception e)
                {
                    log.Error($"PicturePicker error in HandleImagePick: {e.ToLongString()}");
                    tcsSaving.SetResult(false);
                }
                finally
                {
                    image.Dispose();
                }
            }

            NSThread.MainThread.InvokeOnMainThread(() =>
            {
                picker.DismissViewController(true, () =>
                {
                    //picker.Dispose();
                    //Fix still image problem when used twice on iOS 7
                    GC.Collect();

                    if (imageFile != null)
                        tcs.SetResult(true);
                    else
                        tcs.SetResult(false);
                    tcs = null;
                });
            });
        }

        public void Dispose()
        {
            picker?.Dispose();
            savingTaskAction = null;
        }
    }
}
