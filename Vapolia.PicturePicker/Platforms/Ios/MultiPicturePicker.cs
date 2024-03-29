﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using GMImagePicker;
using ImageIO;
using Microsoft.Extensions.Logging;
using MobileCoreServices;
using Photos;
using UIKit;

namespace Vapolia.PicturePicker.PlatformLib
{
    [Foundation.Preserve(AllMembers = true)]
    public class MultiPicturePicker : IMultiPicturePicker
    {
        private readonly ILogger log;
        GMImagePickerController picker; //to prevent dispose

        public MultiPicturePicker(ILogger logger)
        {
            log = logger;
        }

        /// <summary>
        /// Pick multiple pictures
        /// </summary>
        /// <param name="storageFolder">Folder where to save the images</param>
        /// <param name="options"></param>
        /// <returns>List of image filepaths including saveFolder. Filenames are generated as guids.</returns>
        public async Task<List<string>> ChoosePicture(string storageFolder, MultiPicturePickerOptions options = default, CancellationToken cancel = default)
        {
            log.LogTrace("ChoosePicture called");

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
            picker.Canceled += (sender, args) =>
            {
                log.LogTrace("ChoosePicture picker Canceled");
                tcs.TrySetResult(false);
            };
            picker.FinishedPickingAssets += async (sender, args) => 
            { 
                log.LogTrace("ChoosePicture picker FinishedPickingAssets");
                if(args.Assets.Length == 0)
                    tcs.TrySetResult(false);
                else
                {
                    var cancel = new CancellationTokenSource(); //The called can display a "working indicator"
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
                        tcs.TrySetResult(false);
                        log.LogError(e, "Error FinishedPickingAssets");
                    }
                    finally 
                    {
                        cancel.Cancel();
                    }
                }

            };

            var modalHost = GetTopModalHostViewController();
            if (modalHost == null)
            {
                log.LogError("ChoosePicture no modal host available to push this viewcontroller");
                return null;
            }
            try
            {
                await modalHost.PresentViewControllerAsync(picker, true);
                log.LogTrace("ChoosePicture modalHost presented");
            }
            catch(Exception e)
            {
                log.LogError($"ChoosePicture modalHost presented exception: {e.Message}");
                tcs.TrySetResult(false);
            }

            if (!await tcs.Task)
                pictureNames.Clear();

            log.LogTrace($"ChoosePicture finished");
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
                { NetworkAccessAllowed = true, DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat, ResizeMode = PHImageRequestOptionsResizeMode.Exact }, (result, info) =>
            {
                //With the Opportunistic delivery mode, the result block may be called more than once with different sizes
                //if (!info.TryGetValue(new NSString("PHImageResultIsDegradedKey"), out var value) || ((NSNumber)value).Int64Value == 0)
                //{
                    // Do something with the FULL SIZED image
                    tcs.TrySetResult(result);
                //} 
                //else 
                //{
                //    // Do something with the regraded image
                //}
            });

            var tcs0 = new TaskCompletionSource<NSDictionary>();
            PHImageManager.DefaultManager.RequestImageData(asset, new PHImageRequestOptions { NetworkAccessAllowed = true, DeliveryMode = PHImageRequestOptionsDeliveryMode.FastFormat, ResizeMode = PHImageRequestOptionsResizeMode.None }, (nsData, uti, orientation, dictionary) =>
             {
                 var imageSource = CGImageSource.FromData(nsData);
                 var meta = imageSource.CopyProperties(new CGImageOptions(), 0);
                 tcs0.TrySetResult(meta);
             });

            var metadata = await tcs0.Task;
            var image = await tcs.Task;

            var imageDestination = CGImageDestination.Create(new CGDataConsumer(NSUrl.FromFilename(filePath)), UTType.JPEG, 1, new CGImageDestinationOptions {LossyCompressionQuality = (float) (options.PercentQuality / 100.0) });
            metadata = PicturePicker.FixOrientationMetadata(metadata);
            var cgImage = PicturePicker.FixOrientation(image);
            imageDestination.AddImage(cgImage, metadata);
            if (!imageDestination.Close()) //Dispose is called by Close ...
                log.LogError($"MultiPicturePicker: failed to save photo to {filePath}");

            return filePath;
        }


        /// <summary>
        /// Souci qd l'appel est fait d'un controlleur déjà modal: ca le fait disparaitre et par conséquent disparaitre aussi ce controlleur
        /// car GetTopModalHostViewController peux renvoyer un controlleur modal ...
        /// car KeyWindow est une UIAlert window avec un WindowLevel à 200
        /// </summary>
        internal static UIViewController? GetTopModalHostViewController()
        {
            return GetCurrentViewController(false);
            //return Xamarin.Essentials.Platform.GetCurrentViewController(true);

            // var window = UIApplication.SharedApplication.Windows.LastOrDefault(w => w.WindowLevel == UIWindowLevel.Normal);
            // if (window == null)
            //     return null;
            //
            // var uiViewController = window.RootViewController;
            // do
            // {
            //     if (uiViewController.PresentedViewController is UINavigationController)
            //         uiViewController = uiViewController.PresentedViewController;
            // }
            // while (uiViewController.PresentedViewController != null);
            //
            // return uiViewController;
        }
        
        #region From Xamarin.Essentials
        private static UIViewController GetCurrentViewController(bool throwIfNull = true)
        {
            UIViewController viewController = null;

            var window = UIApplication.SharedApplication.KeyWindow;

            if (window != null && window.WindowLevel == UIWindowLevel.Normal)
                viewController = window.RootViewController;

            if (viewController == null)
            {
                window = UIApplication.SharedApplication
                    .Windows
                    .OrderByDescending(w => w.WindowLevel)
                    .FirstOrDefault(w => w.RootViewController != null && w.WindowLevel == UIWindowLevel.Normal);

                if (window == null && throwIfNull)
                    throw new InvalidOperationException("Could not find current view controller.");
                else
                    viewController = window?.RootViewController;
            }

            while (viewController?.PresentedViewController != null)
                viewController = viewController.PresentedViewController;

            if (throwIfNull && viewController == null)
                throw new InvalidOperationException("Could not find current view controller.");

            return viewController;
        }

        private static UIWindow GetCurrentWindow(bool throwIfNull = true)
        {
            var window = UIApplication.SharedApplication.KeyWindow;

            if (window != null && window.WindowLevel == UIWindowLevel.Normal)
                return window;

            if (window == null)
            {
                window = UIApplication.SharedApplication
                    .Windows
                    .OrderByDescending(w => w.WindowLevel)
                    .FirstOrDefault(w => w.RootViewController != null && w.WindowLevel == UIWindowLevel.Normal);
            }

            if (throwIfNull && window == null)
                throw new InvalidOperationException("Could not find current window.");

            return window;
        }
        #endregion
    }
}
