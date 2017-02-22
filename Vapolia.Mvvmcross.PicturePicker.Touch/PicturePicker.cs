using System;
using CoreGraphics;
using System.Threading.Tasks;
using Foundation;
using MvvmCross.Platform;
using MvvmCross.Platform.Core;
using MvvmCross.Platform.Exceptions;
using MvvmCross.Platform.iOS.Platform;
using MvvmCross.Platform.iOS.Views;
using MvvmCross.Plugins.File;
using UIKit;

namespace Vapolia.Mvvmcross.PicturePicker.Touch
{
    [Preserve(AllMembers = true)]
    public sealed class PicturePicker : MvxIosTask, IPicturePicker, IDisposable
    {
        private readonly IMvxIosModalHost modalHost;
        private readonly IMvxFileStore fileStore;
        private readonly UIImagePickerController picker;
        private int maxPixelDimension;
        private int percentQuality;
        private Action<Task<bool>> savingTaskAction;
        private TaskCompletionSource<string> tcs;

        public PicturePicker()
        {
            modalHost = Mvx.Resolve<IMvxIosModalHost>();
            fileStore = Mvx.Resolve<IMvxFileStore>();

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
                picker.Dispose();
                modalHost.NativeModalViewControllerDisappearedOnItsOwn();
                tcs.SetCanceled();
                tcs = null;
            };
        }

        public Task<string> ChoosePictureFromLibrary(Action<Task<bool>> saving = null, int maxPixelDimension = 0, int percentQuality = 80)
        {
            savingTaskAction = saving;
            picker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
            return ChoosePictureCommon(maxPixelDimension, percentQuality);
        }

        public Task<string> TakePicture(Action<Task<bool>> saving = null, int maxPixelDimension = 0, int percentQuality = 0, bool useFrontCamera=false)
        {
            savingTaskAction = saving;
            picker.SourceType = UIImagePickerControllerSourceType.Camera;
            picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Photo;
            picker.CameraDevice = useFrontCamera ? UIImagePickerControllerCameraDevice.Front : UIImagePickerControllerCameraDevice.Rear;
            return ChoosePictureCommon(maxPixelDimension, percentQuality);
        }

        private Task<string> ChoosePictureCommon(int maxPixelDimension, int percentQuality)
        {
            if (tcs != null)
            {
                Mvx.Error("PicturePicker: A call is already in progress");
                return null;
            }
            
            tcs = new TaskCompletionSource<string>();

            this.maxPixelDimension = maxPixelDimension;
            this.percentQuality = percentQuality;
            if (!modalHost.PresentModalViewController(picker, true))
            {
                Mvx.Warning("PicturePicker: PresentModalViewController failed");
                tcs.SetResult(null);
            }

            return tcs.Task;
        }

        private async Task HandleImagePick(UIImage image)
        {
            await Task.Yield();

            string imageFile = null;
            if (image != null)
            {
                var tcsSaving = new TaskCompletionSource<bool>();
                savingTaskAction?.Invoke(tcsSaving.Task);

                try
                {
                    // resize the image
                    if (maxPixelDimension > 0)
                    {
                        var oldImage = image;
                        image = image.ImageToFitSize(new CGSize(maxPixelDimension, maxPixelDimension));
                        oldImage.Dispose();
                    }

                    using (var data = image.AsJPEG((float) (percentQuality/100.0)))
                    {
                        using (var stream = data.AsStream())
                        {
                            imageFile = await ImageHelper.SaveImage(fileStore, stream).ConfigureAwait(false);
                        }
                    }
                    image.Dispose();

                    tcsSaving.SetResult(true);
                }
                catch (Exception e)
                {
                    Mvx.Error("PicturePicker error in HandleImagePick: {0}", e.ToLongString());
                    tcsSaving.SetResult(false);
                }
            }

            MvxSingleton<IMvxMainThreadDispatcher>.Instance.RequestMainThreadAction(() =>
            {
                picker.DismissViewController(true, () =>
                {
                    modalHost.NativeModalViewControllerDisappearedOnItsOwn();
                    picker.Dispose();
                    //Fix still image problem when used twice on iOS 7
                    GC.Collect();


                    if (imageFile != null)
                        tcs.SetResult(imageFile);
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
