using System;
using Vapolia.Mvvmcross.PicturePicker.Lib;

namespace Vapolia.Mvvmcross.PicturePicker
{
    /// <summary>
    /// This class is a shortcut to access the most used exif tag, it also take care of formating the Raw exif data like transforming latitude/longitude from int[3] to double
    /// if a tag is missing use MediaFile TryGetRawTagValue to get your particular exif data.
    /// </summary>
    /// <remarks>
    /// For a reference of ExifTags see : http://www.exiv2.org/tags.html
    /// </remarks>
    [Preserve(AllMembers = true)]
    public sealed class JpegInfoService : JpegInfoServiceBase
    {
        public JpegInfoService(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            Reader = new ExifBinaryReader(filePath);
        }
    }
}
