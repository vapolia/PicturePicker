using System;
using System.Threading;
using CoreGraphics;
using System.Threading.Tasks;
using Foundation;
using MvvmCross.Platform;
using MvvmCross.Platform.Exceptions;
using MvvmCross.Platform.iOS.Platform;
using MvvmCross.Platform.iOS.Views;
using MvvmCross.Platform.Logging;
using UIKit;

namespace Vapolia.Mvvmcross.PicturePicker.Touch
{
    [Preserve(AllMembers = true)]
    public sealed class PicturePicker : MvxIosTask, IPicturePicker, IDisposable
    {
        private readonly IMvxLog log;
        private readonly IMvxIosModalHost modalHost;
        private readonly UIImagePickerController picker;
        // ReSharper disable InconsistentNaming
        private int _maxPixelWidth, _maxPixelHeight;
        private int _percentQuality;
        private string _filePath;
        // ReSharper restore InconsistentNaming
        private Action<Task<bool>> savingTaskAction;
        private TaskCompletionSource<bool> tcs;
        private bool shouldSaveToGallery;

        public PicturePicker(IMvxLog logger, IMvxIosModalHost modalHost)
        {
            log = logger;
            this.modalHost = modalHost;

            picker = new UIImagePickerController();

            picker.FinishedPickingMedia += (sender, args) =>
            {
                var image = args.EditedImage ?? args.OriginalImage;
                Task.Run(() => HandleImagePick(image));
            };

            picker.FinishedPickingImage += (sender, args) =>
            {
                var image = args.Image;
                Task.Run(() => HandleImagePick(image));
            };

            picker.Canceled += async (sender, args) =>
            {
                await picker.DismissViewControllerAsync(true);
                //picker.Dispose();
                modalHost.NativeModalViewControllerDisappearedOnItsOwn();
                tcs.SetCanceled();
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
            picker.AllowsEditing = true;
            picker.AllowsImageEditing = true;
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

            if (!modalHost.PresentModalViewController(picker, true))
            {
                log.Warn("PicturePicker: PresentModalViewController failed");
                tcs.SetResult(false);
            }

            return tcs.Task;
        }

        private async Task HandleImagePick(UIImage image)
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
                    {
                        var oldImage = image;
                        image = image.ImageToFitSize(new CGSize(_maxPixelWidth, _maxPixelHeight));
                        oldImage.Dispose();
                    }

                    using (var data = image.AsJPEG((float) (_percentQuality/100.0)))
                    {
                        using (var stream = data.AsStream())
                        {
                            await ImageHelper.SaveImage(_filePath, stream, CancellationToken.None).ConfigureAwait(false);
                            imageFile = _filePath;
                        }
                    }
                    image.Dispose();

                    tcsSaving.SetResult(true);
                }
                catch (Exception e)
                {
                    log.Error($"PicturePicker error in HandleImagePick: {e.ToLongString()}");
                    tcsSaving.SetResult(false);
                }
            }

            NSThread.MainThread.InvokeOnMainThread(() =>
            {
                picker.DismissViewController(true, () =>
                {
                    modalHost.NativeModalViewControllerDisappearedOnItsOwn();
                    //picker.Dispose();
                    //Fix still image problem when used twice on iOS 7
                    GC.Collect();


                    if (imageFile != null)
                        tcs.SetResult(true);
                    else
                        tcs.SetCanceled();
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
