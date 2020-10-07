//using System;
//using System.Threading.Tasks;

//namespace Vapolia.PicturePicker.Lib
//{
//    /// <summary>
//    /// Shortcut to access the most used exif tag, it also take care of formating the Raw exif data like transforming latitude/longitude from int[3] to double
//    /// if a tag is missing use MediaFile TryGetRawTagValue to get your particular exif data.
//    /// </summary>
//    /// <remarks>
//    /// For a reference of ExifTags see : http://www.exiv2.org/tags.html
//    /// </remarks>
//    public interface IJpegInfo : IDisposable
//    {
//        int PixelWidth { get; }
//        int PixelHeight { get; }
//        string Artist { get; }
//        string ColorModel { get; set; }
//        ExifTagColorSpace? ColorSpace { get; }
//        string Copyright { get; }
//        string DateTime { get; }
//        string DateTimeDigitized { get; }
//        string DateTimeOriginal { get; }
//        double? ExposureTime { get; }
//        ExifTagFlash? Flash { get; }
//        double? FNumber { get; }
//        double? GpsAltitude { get; }
//        ExifTagGpsAltitudeRef? GpsAltitudeRef { get; }
//        double? GpsDestBearing { get; }
//        ExifTagGpsBearingRef? GpsDestBearingRef { get; }
//        double? GpsLatitude { get; }
//        ExifTagGpsLatitudeRef? GpsLatitudeRef { get; }
//        double? GpsLongitude { get; }
//        ExifTagGpsLongitudeRef? GpsLongitudeRef { get; }
//        double? GpsSpeed { get; }
//        ExifTagGpsSpeedRef? GpsSpeedRef { get; }
//        string ImageDescription { get; }
//        string Make { get; }
//        string Model { get; }
//        ExifTagOrientation? Orientation { get; }
//        uint? PixelXDimension { get; }
//        uint? PixelYDimension { get; }
//        ExifTagResolutionUnit? ResolutionUnit { get; }
//        string Software { get; }
//        bool TryGetRawTagValue<T>(ExifTags tag, out T result);

//        Task TrySetGpsData(string ressourcePath, double latitude, double longitude, double altitude);
//        string UserComment { get; }
//        double? XResolution { get; }
//        double? YResolution { get; }
//    }
//}
