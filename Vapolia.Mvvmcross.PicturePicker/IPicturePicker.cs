using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vapolia.Mvvmcross.PicturePicker
{
    public interface IPicturePicker
    {
        /// <summary>
        /// Returns null if cancelled
        /// </summary>
        Task<string> ChoosePictureFromLibrary(Action<Task<bool>> saving = null, int maxPixelDimension=0, int percentQuality=80);

        /// <summary>
        /// Returns null if cancelled
        /// </summary>
        Task<string> TakePicture(Action<Task<bool>> saving = null, int maxPixelDimension=0, int percentQuality=0, bool useFrontCamera=false);
    }
}
