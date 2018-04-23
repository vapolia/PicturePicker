using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vapolia.Mvvmcross.PicturePicker
{
    public interface IMultiPicturePicker
    {
        Task<List<string>> ChoosePicture(string storageFolder, MultiPicturePickerOptions options = null);
    }
}