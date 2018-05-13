using System;
using System.Linq;
using MvvmCross;
using MvvmCross.Plugin;

namespace Vapolia.Mvvmcross.PicturePicker.Droid
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class Plugin : IMvxPlugin
    {
        public void Load()
        {
            Mvx.RegisterType<IPicturePicker, PicturePicker>();
            //Mvx.RegisterType<IExifReader, ExifBinaryReader>();
            //Mvx.RegisterType<IJpegInfo, JpegInfo>();
        }
    }
}
