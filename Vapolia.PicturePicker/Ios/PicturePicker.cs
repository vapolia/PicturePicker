using System;
using System.IO;
using System.Threading;
using CoreGraphics;
using System.Threading.Tasks;
using Foundation;
using ImageIO;
using Microsoft.Extensions.Logging;
using MobileCoreServices;
using Photos;
using UIKit;
using Xamarin.Essentials;

namespace Vapolia.PicturePicker.PlatformLib
{
    [Preserve(AllMembers = true)]
    public sealed class PicturePicker : IPicturePicker
    {
        private readonly ILogger log;
        // ReSharper disable InconsistentNaming
        private int _maxPixelWidth, _maxPixelHeight;
        private int _percentQuality;
        private string _filePath;
        // ReSharper restore InconsistentNaming
        private TaskCompletionSource<bool>? tcs;
        private bool shouldSaveToGallery;

        public bool HasCamera => UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera)
                                 || UIImagePickerController.IsCameraDeviceAvailable(UIImagePickerControllerCameraDevice.Front)
                                 || UIImagePickerController.IsCameraDeviceAvailable(UIImagePickerControllerCameraDevice.Rear);

        public PicturePicker(ILogger logger)
        {
            log = logger;
        }

        public Task<bool> ChoosePictureFromLibrary(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality = 80)
        {
            var picker = CreatePicker(saving);
            picker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
            picker.AllowsEditing = true;
            picker.AllowsImageEditing = true;
            shouldSaveToGallery = false;
            return ChoosePictureCommon(picker, filePath, maxPixelWidth, maxPixelHeight, percentQuality);
        }

        public Task<bool> TakePicture(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality = 0, bool useFrontCamera=false, bool saveToGallery=false, CancellationToken cancel = default)
        {
            if (!HasCamera)
            {
                log.LogWarning("Source type Camera not available on this device.");
                return Task.FromResult(false);
            }

            var camera = useFrontCamera ? UIImagePickerControllerCameraDevice.Front : UIImagePickerControllerCameraDevice.Rear;
            if (!UIImagePickerController.IsCameraDeviceAvailable(camera))
            {
                camera = useFrontCamera ? UIImagePickerControllerCameraDevice.Rear : UIImagePickerControllerCameraDevice.Front;
                if (!UIImagePickerController.IsCameraDeviceAvailable(camera))
                {
                    log.LogWarning("No camera available on this device.");
                    return Task.FromResult(false);
                }
            }

            var picker = CreatePicker(saving);
            shouldSaveToGallery = saveToGallery;
            picker.SourceType = UIImagePickerControllerSourceType.Camera;
            picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Photo;
            picker.CameraDevice = useFrontCamera ? UIImagePickerControllerCameraDevice.Front : UIImagePickerControllerCameraDevice.Rear;
            picker.AllowsEditing = false;
            picker.AllowsImageEditing = false;
            picker.Editing = false;
            return ChoosePictureCommon(picker, filePath, maxPixelWidth, maxPixelHeight, percentQuality);
        }

        private async Task<bool> ChoosePictureCommon(UIImagePickerController picker, string filePath, int maxPixelWidth, int maxPixelHeight, int percentQuality)
        {
            if (tcs != null)
            {
                log.LogError("PicturePicker: A call is already in progress");
                return false;
            }

            var modalHost = MultiPicturePicker.GetTopModalHostViewController();
            if (modalHost == null)
            {
                log.LogError("ChoosePicture no modal host available to push this viewcontroller");
                return false;
            }

            _maxPixelWidth = maxPixelWidth;
            _maxPixelHeight = maxPixelHeight;
            _percentQuality = percentQuality;
            _filePath = filePath;

            if (DeviceInfo.Idiom == DeviceIdiom.Tablet && picker.PopoverPresentationController != null && modalHost.View != null)
                picker.PopoverPresentationController.SourceRect = modalHost.View.Bounds;
            
            tcs = new TaskCompletionSource<bool>();
            await modalHost.PresentViewControllerAsync(picker, true);
            var ok = await tcs.Task;
            tcs = null;
            picker.Dispose();
            return ok;
        }

        private UIImagePickerController CreatePicker(Action<Task<bool>>? saving = null)
        {
            var picker = new UIImagePickerController();

            picker.FinishedPickingMedia += async (sender, args) =>
            {
                var image = args.EditedImage ?? args.OriginalImage;
                var metadata = args.MediaMetadata;

                if (metadata == null)
                {
                    //Requiert l'acces à la photo lib
                    //var asset = args.PHAsset; //ios11+
                    var asset = (PHAsset?)PHAsset.FetchAssets( new [] { args.ReferenceUrl ?? args.ImageUrl ?? args.MediaUrl }, new PHFetchOptions { IncludeAssetSourceTypes = PHAssetSourceType.UserLibrary|PHAssetSourceType.iTunesSynced|PHAssetSourceType.CloudShared }).firstObject; //ios8-10

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
                    await using var stream = data.AsStream();
                    var info = new JpegInfoService(stream);
                    var lat = info.GpsLatitude;
                    var lng = info.GpsLongitude;
                }
#endif

                var saved = await HandleImagePick(image, metadata, saving);

                
                NSThread.MainThread.InvokeOnMainThread(() =>
                {
                    picker.DismissViewController(true, () =>
                    {
                        //Fix still image problem when used twice on iOS 7
                        GC.Collect();

                        tcs?.TrySetResult(saved);
                    });
                });
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
                tcs?.TrySetResult(false);
            };

            //iOS 13 only
            if (DeviceInfo.Version.Major >= 13)
            {
                var ad = new ModalDelegate();
                ad.Dismissed += (sender, args) =>
                {
                    tcs?.TrySetResult(false);
                };
                picker.PresentationController.Delegate = ad;
            }

            return picker;
        }

        class ModalDelegate : UIAdaptivePresentationControllerDelegate
        {
            public event EventHandler<EventArgs> Dismissed; 
            
            //Default is to dismiss. Comment this out to prevent default.
            // public override void DidAttemptToDismiss(UIPresentationController presentationController)
            // {
            //     
            // }

            /// <summary>
            /// Called when dismiss 
            /// </summary>
            /// <param name="presentationController"></param>
            /// <returns>false to prevent dismissal</returns>
            public override bool ShouldDismiss(UIPresentationController presentationController)
            {
                return true;
            }


            public override void DidDismiss(UIPresentationController presentationController)
            {
                Dismissed?.Invoke(this, EventArgs.Empty);
            }

            public override void WillDismiss(UIPresentationController presentationController)
            {
            }
        }

        private async Task<bool> HandleImagePick(UIImage? image, NSDictionary? metadata, Action<Task<bool>>? savingTaskAction)
        {
            string? imageFile = null;
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

                    // rotate the CgImage if the orientation is not already good
                    var imageDestination = CGImageDestination.Create(new CGDataConsumer(NSUrl.FromFilename(_filePath)), UTType.JPEG, 1, new CGImageDestinationOptions {LossyCompressionQuality = (float) (_percentQuality / 100.0)});
                    metadata = FixOrientationMetadata(metadata);
                    var cgImage = FixOrientation(image);
                    imageDestination.AddImage(cgImage, metadata);
                    if (!imageDestination.Close()) //Dispose is called by Close ...
                        log.LogError("PicturePicker: failed to copy photo on save to {0}", _filePath);
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
                    log.LogError(e, "PicturePicker error in HandleImagePick");
                    tcsSaving.SetResult(false);
                }
                finally
                {
                    image.Dispose();
                }
            }

            return imageFile != null;
        }

        internal static NSDictionary FixOrientationMetadata(NSDictionary metadata)
        {
            if (metadata.TryGetValue(new NSString("Orientation"), out var orientation))
            {
                metadata = (NSMutableDictionary)metadata.MutableCopy();

                metadata["Orientation"] = new NSNumber(1);
                if (metadata.TryGetValue(new NSString("{TIFF}"), out var tiff))
                {
                    var tiffDic = (NSDictionary)tiff;
                    if (tiffDic.TryGetValue(new NSString("Orientation"), out orientation))
                    {
                        var tiffDicMutable = (NSMutableDictionary)tiffDic.MutableCopy();
                        tiffDicMutable["Orientation"] = new NSNumber(1);
                        metadata["{TIFF}"] = tiffDicMutable;
                    }
                }
            }

            return metadata;
        }

        internal static CGImage FixOrientation(UIImage image)
        {
            var orientation = image.Orientation;
            if (orientation == UIImageOrientation.Up)
                return image.CGImage;

            // We need to calculate the proper transformation to make the image upright.
            // We do it in 2 steps: Rotate if Left/Right/Down, and then flip if Mirrored.
            var transform = CGAffineTransform.MakeIdentity();
            var size = image.Size;
            switch (orientation) 
            {
                case UIImageOrientation.Down:
                case UIImageOrientation.DownMirrored:
                    transform = CGAffineTransform.Translate(transform, size.Width, size.Height);
                    transform = CGAffineTransform.Rotate(transform, (nfloat)Math.PI);
                    break;

                case UIImageOrientation.Left:
                case UIImageOrientation.LeftMirrored:
                    transform = CGAffineTransform.Translate(transform, size.Width, 0);
                    transform = CGAffineTransform.Rotate(transform, (nfloat)Math.PI/2);
                    break;

                case UIImageOrientation.Right:
                case UIImageOrientation.RightMirrored:
                    transform = CGAffineTransform.Translate(transform, 0, size.Height);
                    transform = CGAffineTransform.Rotate(transform, (nfloat)(-Math.PI/2));
                    break;
            }

            switch (orientation) 
            {
                case UIImageOrientation.UpMirrored:
                case UIImageOrientation.DownMirrored:
                    transform = CGAffineTransform.Translate(transform, size.Width, 0);
                    transform = CGAffineTransform.Scale(transform, -1, 1);
                    break;

                case UIImageOrientation.LeftMirrored:
                case UIImageOrientation.RightMirrored:
                    transform = CGAffineTransform.Translate(transform, size.Height, 0);
                    transform = CGAffineTransform.Scale(transform, -1, 1);
                    break;
            }
            
            // Now we draw the underlying CGImage into a new context, applying the transform calculated above.
            var ctx = new CGBitmapContext(null, (nint)size.Width, (nint)size.Height, image.CGImage.BitsPerComponent, 0, image.CGImage.ColorSpace, image.CGImage.BitmapInfo);
            ctx.ConcatCTM(transform);
            switch (orientation) 
            {
                case UIImageOrientation.Left:
                case UIImageOrientation.LeftMirrored:
                case UIImageOrientation.Right:
                case UIImageOrientation.RightMirrored:
                    // Grr...
                    ctx.DrawImage(new CGRect(0,0,size.Height,size.Width), image.CGImage);
                    break;

                default:
                    ctx.DrawImage(new CGRect(0,0,size.Width,size.Height), image.CGImage);
                    break;
            }

            var cgimg = ctx.ToImage();
            ctx.Dispose();
            return cgimg;
        }
    }
}
