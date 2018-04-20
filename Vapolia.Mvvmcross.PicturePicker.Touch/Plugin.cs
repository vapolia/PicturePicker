using System;
using System.Linq;
using Foundation;
using MvvmCross.Platform;
using MvvmCross.Platform.Plugins;
using Vapolia.Mvvmcross.PicturePicker.Lib;

namespace Vapolia.Mvvmcross.PicturePicker.Touch
{
    [Preserve(AllMembers = true)]
    public class Plugin : IMvxPlugin
    {
        public void Load()
        {
            Mvx.RegisterType<IPicturePicker, PicturePicker>();
            //Mvx.RegisterType<IJpegInfo, JpegInfo>();
        }
    }
}
