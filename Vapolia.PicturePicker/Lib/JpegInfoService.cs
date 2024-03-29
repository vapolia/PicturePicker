using Vapolia.PicturePicker.Lib;

namespace Vapolia.PicturePicker;

/// <summary>
/// This class is a shortcut to access the most used exif tag, it also take care of formating the Raw exif data like transforming latitude/longitude from int[3] to double
/// if a tag is missing use MediaFile TryGetRawTagValue to get your particular exif data.
/// </summary>
/// <remarks>
/// For a reference of ExifTags see : http://www.exiv2.org/tags.html
/// </remarks>
public sealed class JpegInfoService : JpegInfoServiceBase
{
    public JpegInfoService(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) 
            throw new ArgumentNullException(nameof(filePath));
        Reader = new ExifBinaryReader(filePath);
    }

    //public JpegInfoService(byte[] jpegData)
    //{
    //    if (jpegData == null || jpegData.Length == 0)
    //        throw new ArgumentNullException(nameof(jpegData));
    //    Reader = new ExifBinaryReader(jpegData);
    //}

    public JpegInfoService(Stream jpegData)
    {
        if (jpegData == null || !jpegData.CanRead)
            throw new ArgumentNullException(nameof(jpegData));
        Reader = new(jpegData);
    }
}