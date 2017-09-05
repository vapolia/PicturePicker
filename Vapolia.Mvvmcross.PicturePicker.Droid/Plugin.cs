using System;
using System.Linq;
using MvvmCross.Platform;
using MvvmCross.Platform.Plugins;
using Android.Runtime;

namespace Vapolia.Mvvmcross.PicturePicker.Droid
{
    [Preserve(AllMembers = true)]
    public class Plugin : IMvxPlugin
    {
        public void Load()
        {
            Mvx.RegisterType<IPicturePicker, PicturePicker>();
        }
    }
}
