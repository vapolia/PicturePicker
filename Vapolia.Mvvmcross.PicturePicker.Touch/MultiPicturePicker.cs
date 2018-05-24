using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using GMImagePicker;
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
    public class MultiPicturePicker : IMultiPicturePicker
    {
        private readonly IMvxLog log;
        GMImagePickerController picker; //to prevent dispose

        public MultiPicturePicker(IMvxLog logger)
        {
            log = logger;
        }

        /// <summary>
        /// Pick multiple pictures
        /// </summary>
        /// <param name="storageFolder">Folder where to save the images</param>
        /// <param name="options"></param>
        /// <returns>List of image filepaths including saveFolder. Filenames are generated as guids.</returns>
        public async Task<List<string>> ChoosePicture(string storageFolder, MultiPicturePickerOptions options = null)
        {
            if(options == null)
                options = new MultiPicturePickerOptions();

            var tcs = new TaskCompletionSource<bool>();
            var pictureNames = new List<string>();

            picker = new GMImagePickerController
            {
                DisplaySelectionInfoToolbar = true,
                DisplayAlbumsNumberOfAssets = true,
                ShowCameraButton = false,
                MediaTypes = new [] { PHAssetMediaType.Image },
                GridSortOrder = SortOrder.Descending,

                Title = options.Title,
                CustomNavigationBarPrompt = options.NavigationBarPrompt,
                CustomDoneButtonTitle = options.DoneButtonTitle,
                CustomCancelButtonTitle = options.CancelButtonTitle,
                CustomPhotosAccessDeniedErrorTitle = options.PhotosAccessDeniedErrorTitle,

                //PickerBackgroundColor = UIColor.Black;
                //PickerTextColor = UIColor.White;
                //ToolbarBarTintColor = UIColor.DarkGray;
                //ToolbarTextColor = UIColor.White;
                //ToolbarTintColor = UIColor.Red;
                //NavigationBarBackgroundColor = UIColor.Black;
                //NavigationBarTextColor = UIColor.White;
                //NavigationBarTintColor = UIColor.Red;
                //PickerFontName = "Verdana";
                //PickerBoldFontName = "Verdana-Bold";
                //PickerFontNormalSize = 14.0f;
                //PickerFontHeaderSize = 17.0f;
                //PickerStatusBarStyle = UIStatusBarStyle.LightContent;
                //UseCustomFontForNavigationBar = true;
            };
            picker.Canceled += (sender, args) => tcs.TrySetResult(false);
            picker.FinishedPickingAssets += async (sender, args) => 
            { 
                if(args.Assets.Length == 0)
                    tcs.TrySetResult(false);
                else
                {
                    var cancel = new CancellationTokenSource();
                    options.SavingAction?.Invoke(cancel.Token);
                    try
                    {
                        foreach (var phAsset in args.Assets)
                        {
                            var pictureName = await RescaleAndSaveAsset(storageFolder, phAsset, options);
                            if(pictureName != null)
                                pictureNames.Add(pictureName);
                        }
                        tcs.TrySetResult(true);
                    }
                    catch(Exception e)
                    {
                        log.Error(e.ToLongString());
                        tcs.TrySetResult(false);
                    }
                    finally 
                    {
                        cancel.Cancel();
                    }
                }

            };

            var modalHost = UIApplication.SharedApplication.KeyWindow.GetTopModalHostViewController();
            await modalHost.PresentViewControllerAsync(picker, true);

            if (!await tcs.Task)
                pictureNames.Clear();

            picker = null;
            return pictureNames;
        }

        private async Task<string> RescaleAndSaveAsset(string storageFolder, PHAsset asset, MultiPicturePickerOptions options)
        {
            var fitSize = new CGSize(options.MaxPixelWidth, options.MaxPixelHeight);
            double width = asset.PixelWidth;
            double height = asset.PixelHeight;
            if ((fitSize.Width > 0 && width > fitSize.Width) || (fitSize.Height > 0 && height > fitSize.Height))
                ImageHelper.Scale(ref width, ref height, fitSize.Width, fitSize.Height);
            var filePath = Path.Combine(storageFolder, Guid.NewGuid().ToString("N") + ".jpg");

            var tcs = new TaskCompletionSource<UIImage>();
            PHImageManager.DefaultManager.RequestImageForAsset(asset, new CGSize(width, height), PHImageContentMode.AspectFit, new PHImageRequestOptions
                { NetworkAccessAllowed = false, DeliveryMode = PHImageRequestOptionsDeliveryMode.Opportunistic, ResizeMode = PHImageRequestOptionsResizeMode.Exact }, (result, info) =>
            {
                tcs.TrySetResult(result);
            });

            var tcs0 = new TaskCompletionSource<NSDictionary>();
            PHImageManager.DefaultManager.RequestImageData(asset, new PHImageRequestOptions { NetworkAccessAllowed = false, DeliveryMode = PHImageRequestOptionsDeliveryMode.Opportunistic, ResizeMode = PHImageRequestOptionsResizeMode.None }, (nsData, uti, orientation, dictionary) =>
             {
                 var imageSource = CGImageSource.FromData(nsData);
                 var meta = imageSource.CopyProperties(new CGImageOptions(), 0);
                 tcs0.TrySetResult(meta);
             });

            var metadata = await tcs0.Task;
            var image = await tcs.Task;

            var imageDestination = CGImageDestination.Create(new CGDataConsumer(NSUrl.FromFilename(filePath)), UTType.JPEG, 1, new CGImageDestinationOptions {LossyCompressionQuality = (float) (options.PercentQuality / 100.0)});
            imageDestination.AddImage(image.CGImage, metadata);
            if (!imageDestination.Close()) //Dispose is called by Close ...
                log.Error($"MultiPicturePicker: failed to save photo to {filePath}");

            return filePath;
        }
    }
}
