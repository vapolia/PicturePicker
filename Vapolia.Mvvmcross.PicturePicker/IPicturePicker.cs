using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vapolia.Mvvmcross.PicturePicker
{
    public interface IPicturePicker
    {
        Task<bool> ChoosePictureFromLibrary(string filePath, Action<Task<bool>> saving = null, int maxPixelDimension=0, int percentQuality=80);

        /// <summary>
        /// Returns null if cancelled
        /// saveToGallery can fails silently
        /// </summary>
        Task<bool> TakePicture(string filePath, Action<Task<bool>> saving = null, int maxPixelDimension=0, int percentQuality=0, bool useFrontCamera=false, bool saveToGallery=false);

        bool HasCamera { get; }
    }
}
