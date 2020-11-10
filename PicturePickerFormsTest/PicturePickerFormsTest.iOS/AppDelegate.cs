using Foundation;
using Microsoft.Extensions.Logging;
using SimpleInjector;
using UIKit;
using Vapolia.PicturePicker;
using Vapolia.PicturePicker.PlatformLib;

namespace PicturePickerFormsTest.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();

            var logger = LoggerFactory.Create(builder =>
            {
#if DEBUG
                builder.AddConsole();
#endif
            }).CreateLogger<App>();

            App.Container.RegisterInstance<ILogger>(logger);
            App.Container.Register<IPicturePicker, PicturePicker>(Lifestyle.Singleton);
            
            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }
    }
}