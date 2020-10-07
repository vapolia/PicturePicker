using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Vapolia.PicturePicker;

namespace PicturePickerTests.Droid.ViewModels
{
    public class HomeViewModel : MvxViewModel
    {
        private string displayedImagePath;

        public ICommand TakePictureCommand => new MvxAsyncCommand(TakePicture);
        public ICommand PickPicturesCommand => new MvxAsyncCommand(PickPictures);
        public ICommand PickPictureCommand => new MvxAsyncCommand(PickPicture);

        public string ImagePath
        {
            get => displayedImagePath;
            set => SetProperty(ref displayedImagePath, value);
        }

        private async Task TakePicture()
        {
            var pathBest = global::Android.App.Application.Context.GetExternalFilesDir(null);
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            var imagePath = Path.Combine(path, Guid.NewGuid().ToString("N") + ".jpg");

            var picker = (IPicturePicker)Mvx.IoCProvider.IoCConstruct<Vapolia.PicturePicker.PlatformLib.PicturePicker>();
            var ok = await picker.TakePicture(imagePath, saveToGallery: true);
            if (ok)
                ImagePath = "file://" + imagePath;
        }

        
        private async Task PickPictures()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);

            var picker = (IMultiPicturePicker)Mvx.IoCProvider.IoCConstruct<Vapolia.PicturePicker.PlatformLib.PicturePicker>();
            var images = await picker.ChoosePicture(path);
            if (images.Count > 0)
                ImagePath = "file://" + images[0];
        }
        
        private async Task PickPicture()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            var imagePath = Path.Combine(path, Guid.NewGuid().ToString("N") + ".jpg");

            var picker = (IPicturePicker)Mvx.IoCProvider.IoCConstruct<Vapolia.PicturePicker.PlatformLib.PicturePicker>();
            var ok = await picker.ChoosePictureFromLibrary(imagePath);
            if (ok)
                ImagePath = "file://" + imagePath;
        }
    }
}
