using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Vapolia.PicturePicker
{
    public static partial class AdvancedMediaPicker
    {
        static Task<bool> PlatformChoosePictureFromLibrary(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality=80)
            => throw new NotImplementedInReferenceAssemblyException();

        /// <summary>
        /// Returns null if cancelled
        /// saveToGallery can fails silently
        /// </summary>
        static Task<bool> PlatformTakePicture(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality=0, bool useFrontCamera=false, bool saveToGallery=false, CancellationToken cancel = default)
            => throw new NotImplementedInReferenceAssemblyException();

        static bool PlatformHasCamera 
            => throw new NotImplementedInReferenceAssemblyException();
    }
}