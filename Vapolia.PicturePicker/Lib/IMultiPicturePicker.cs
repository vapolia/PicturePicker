using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vapolia.PicturePicker
{
    public interface IMultiPicturePicker
    {
        Task<List<string>> ChoosePicture(string storageFolder, MultiPicturePickerOptions options = default, CancellationToken cancel = default);
    }
}