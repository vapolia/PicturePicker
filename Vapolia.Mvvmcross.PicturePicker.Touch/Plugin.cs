using MvvmCross;
using MvvmCross.Plugin;

namespace Vapolia.Mvvmcross.PicturePicker.Touch
{
    [MvxPlugin]
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
