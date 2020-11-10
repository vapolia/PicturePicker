using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Vapolia.PicturePicker;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace PicturePickerFormsTest
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private string imagePath;

        public ICommand OpenPictureLibraryCommand { get; }

        public string ImagePath
        {
            get => imagePath;
            set { imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
        }

        public MainPageViewModel(Page page)
        {
            OpenPictureLibraryCommand = new Command(async () =>
            {
                bool ok = false;
                var pictureCacheFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var targetFile = Path.Combine(pictureCacheFolder, $"profilePic-{Guid.NewGuid()}.jpg");

                var hasPermission = await Permissions.CheckStatusAsync<Permissions.Photos>();
                if(hasPermission != PermissionStatus.Granted && hasPermission != PermissionStatus.Restricted)
                    hasPermission = await Permissions.RequestAsync<Permissions.Photos>();
                if (hasPermission != PermissionStatus.Granted && hasPermission != PermissionStatus.Restricted)
                {
                    if(await page.DisplayAlert("Denied", "You denied access to your photo library.", "Open Settings", "OK"))
                        AppInfo.ShowSettingsUI();
                }
                else
                    ok = await AdvancedMediaPicker.ChoosePictureFromLibrary(targetFile, maxPixelWidth: 500, maxPixelHeight: 500);

                if (ok)
                    ImagePath = targetFile;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
