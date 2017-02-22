using MvvmCross.Platform;
using MvvmCross.Platform.Plugins;

namespace Vapolia.Mvvmcross.PicturePicker
{
    [Preserve(AllMembers = true)]
    public class PluginLoader : IMvxPluginLoader
    {
        public static readonly PluginLoader Instance = new PluginLoader();

        public void EnsureLoaded()
        {
            var manager = Mvx.Resolve<IMvxPluginManager>();
            manager.EnsurePlatformAdaptionLoaded<PluginLoader>();
        }
    }
}
