using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Vapolia.Mvvmcross.PicturePicker.Droid;

namespace PicturePickerTests.Droid
{
    public class HomeViewModel : MvxViewModel
    {
        private string displayedImagePath;

        public ICommand TakePictureCommand => new MvxAsyncCommand(TakePicture);

        public string ImagePath
        {
            get => displayedImagePath;
            set => SetProperty(ref displayedImagePath, value);
        }

        private async Task TakePicture()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
            var imagePath = Path.Combine(path, Guid.NewGuid().ToString("N") + ".jpg");

            var picker = Mvx.IoCConstruct<PicturePicker>();
            var ok = await picker.TakePicture(imagePath);
            if (ok)
                ImagePath = "file://" + imagePath;
        }
    }
}
