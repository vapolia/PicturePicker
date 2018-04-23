using Foundation;
using MvvmCross.Platform;
using MvvmCross.Platform.Plugins;

namespace Vapolia.Mvvmcross.PicturePicker.Touch
{
    [Preserve(AllMembers = true)]
    public class Plugin : IMvxPlugin
    {
        public void Load()
        {
            Mvx.RegisterType<IPicturePicker, PicturePicker>();
            Mvx.RegisterType<IMultiPicturePicker, MultiPicturePicker>();
        }
    }
}
