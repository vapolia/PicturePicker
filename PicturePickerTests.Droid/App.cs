using MvvmCross.ViewModels;

namespace PicturePickerTests.Droid
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            RegisterAppStart<HomeViewModel>();
        }
    }
}
