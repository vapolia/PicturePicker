using Foundation;
using MvvmCross.Platform.Plugins;

namespace $rootnamespace$.Bootstrap
{
    [Preserve(AllMembers = true)]
    public class PicturePickerPluginBootstrap
        : MvxLoaderPluginBootstrapAction<Vapolia.Mvvmcross.PicturePicker.PluginLoader, Vapolia.Mvvmcross.PicturePicker.Touch.Plugin>
    {
    }
}
