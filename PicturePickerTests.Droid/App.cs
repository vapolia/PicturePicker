using System.Threading.Tasks;
using Android.App;
using Microsoft.Extensions.Logging;
using MvvmCross;
using MvvmCross.ViewModels;
using PicturePickerTests.Droid.ViewModels;

[assembly:UsesPermission(Android.Manifest.Permission.WriteExternalStorage, MaxSdkVersion = 28)]

namespace PicturePickerTests.Droid
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            RegisterAppStart<HomeViewModel>();
        }

        public override async Task Startup()
        {
            await base.Startup();
            
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<App>();
            Mvx.IoCProvider.RegisterSingleton<ILogger>(logger);
        }
    }
}
