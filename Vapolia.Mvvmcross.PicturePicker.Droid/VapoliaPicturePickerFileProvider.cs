using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.Content;

namespace Vapolia.Mvvmcross.PicturePicker.Droid
{
    [ContentProvider(new[] { "@string/vapolia_picturepicker_fileprovidername" }, Exported = false, GrantUriPermissions = true)]
    [MetaData("android.support.FILE_PROVIDER_PATHS", Resource = "@xml/vapolia_picturepicker_paths")]
    [Preserve]
    public class VapoliaPicturePickerFileProvider : FileProvider
    {
        [Preserve]
        public VapoliaPicturePickerFileProvider()
        {
        }

        [Preserve]
        protected VapoliaPicturePickerFileProvider(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }
    }
}