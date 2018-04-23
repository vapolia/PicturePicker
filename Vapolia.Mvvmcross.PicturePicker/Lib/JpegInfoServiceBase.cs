using System;
using System.Globalization;

namespace Vapolia.Mvvmcross.PicturePicker.Lib
{
    public abstract class JpegInfoServiceBase
    {
        protected ExifBinaryReader Reader;

        #region exif properties
        /// <summary>
        /// Information specific to compressed data. When a compressed file is recorded, the valid height of the meaningful image must be recorded in this tag, whether or not there is padding data or a restart marker. This tag should not exist in an uncompressed file. Since data padding is unnecessary in the vertical direction, 
        /// the number of lines recorded in this valid image height tag will in fact be the same as that recorded in the SOF.
        /// </summary>
        public UInt32? PixelYDimension => Reader.TryGetTagValue<uint>(ExifTags.PixelYDimension, out var result) ?  (uint?)result :null;

        /// <summary>
        /// Information specific to compressed data. When a compressed file is recorded, the valid width of the meaningful image must be recorded in this tag, whether or not there is padding data or a restart marker. This tag should not exist in an uncompressed file.
        /// </summary>
        public UInt32? PixelXDimension => Reader.TryGetTagValue(ExifTags.PixelXDimension, out uint result) ? (uint?)result : null;

        public string ColorModel { get; set; }

        /// <summary>
        /// The color space information tag is always recorded as the color space specifier. Normally sRGB is used to define the color space based on the PC monitor conditions and environment. If a color space other than sRGB is used, Uncalibrated is set. Image data recorded as Uncalibrated can be treated as sRGB when it is converted to FlashPix.
        /// </summary>
        public ExifTagColorSpace? ColorSpace => Reader.TryGetTagValue<ushort>(ExifTags.ColorSpace, out var result) ? (ExifTagColorSpace?)result : null;

        /// <summary>
        /// The date and time when the image was stored as digital data. (local date)
        /// </summary>
        public string DateTimeDigitized => Reader.TryGetTagValue<string>(ExifTags.DateTimeDigitized, out var result) ? result : null;

        /// <summary>
        /// Orientation of the image.
        /// </summary>
        public ExifTagOrientation? Orientation => Reader.TryGetTagValue<ushort>(ExifTags.Orientation, out var result) ? (ExifTagOrientation?)result : null;

        /// <summary>
        /// The number of pixels per ResolutionUnit in the ImageWidth direction. When the image resolution is unknown, 72 [dpi] is designated.
        /// </summary>
        public double? XResolution => Reader.TryGetTagValue<double>(ExifTags.XResolution, out var result) ? (double?)result : null;

        /// <summary>
        /// The number of pixels per ResolutionUnit in the ImageLength direction. The same value as XResolution is designated.
        /// </summary>
        public double? YResolution => Reader.TryGetTagValue<double>(ExifTags.YResolution, out var result) ? (double?)result : null;

        /// <summary>
        /// Resolution unit of the image.
        /// </summary>
        public ExifTagResolutionUnit? ResolutionUnit => Reader.TryGetTagValue<ushort>(ExifTags.ResolutionUnit, out var result) ? (ExifTagResolutionUnit?) result : null;

        /// <summary>
        /// Date at which the image was taken. (local date)
        /// </summary>
        public string DateTime => Reader.TryGetTagValue<string>(ExifTags.DateTime, out var result) ? result : null;


        /// <summary>
        /// Date at which the image was taken. Created by Lumia devices. (local date)
        /// </summary>
        public string DateTimeOriginal => Reader.TryGetTagValue<string>(ExifTags.DateTimeOriginal, out var result) ? result : null;

        /// <summary>
        /// Description of the image.
        /// </summary>
        public string ImageDescription => Reader.TryGetTagValue<string>(ExifTags.ImageDescription, out var result) ? result : null;

        /// <summary>
        /// Camera manufacturer.
        /// </summary>
        public string Make => Reader.TryGetTagValue<string>(ExifTags.Make, out var result) ? result : null;

        /// <summary>
        /// Camera model.
        /// </summary>
        public string Model => Reader.TryGetTagValue<string>(ExifTags.Model, out var result) ? result : null;

        /// <summary>
        /// Software used to create the image.
        /// </summary>
        public string Software => Reader.TryGetTagValue<string>(ExifTags.Software, out var result) ? result : null;

        /// <summary>
        /// Image artist.
        /// </summary>
        public string Artist => Reader.TryGetTagValue<string>(ExifTags.Artist, out var result) ? result : null;

        /// <summary>
        /// Image copyright.
        /// </summary>
        public string Copyright => Reader.TryGetTagValue<string>(ExifTags.Copyright, out var result) ? result : null;

        /// <summary>
        /// Image user comments.
        /// </summary>
        public string UserComment => Reader.TryGetTagValue<string>(ExifTags.UserComment, out var result) ? result : null;

        /// <summary>
        /// Exposure time, in seconds.
        /// </summary>
        public double? ExposureTime => Reader.TryGetTagValue<double>(ExifTags.ExposureTime, out var result) ? (double?)result : null;

        /// <summary>
        /// F-number (F-stop) of the camera lens when the image was taken.
        /// </summary>
        public double? FNumber => Reader.TryGetTagValue<double>(ExifTags.FNumber, out var result) ? (double?)result : null;

        /// <summary>
        /// Flash settings of the camera when the image was taken.
        /// </summary>
        public ExifTagFlash? Flash => Reader.TryGetTagValue<ushort>(ExifTags.Flash, out var result) ? (ExifTagFlash?)result : null;


        #region GPS Data
        public double? GpsLatitude => (Reader.TryGetTagValue(ExifTags.GPSLatitude, out double[] result2) && GpsLatitudeRef.HasValue)
            ? (double?)ExifLatCoordinateToDouble(result2[0], result2[1], result2[2], GpsLatitudeRef.Value)
            : null;

        public double? GpsLongitude => (Reader.TryGetTagValue(ExifTags.GPSLongitude, out double[] result2) && GpsLongitudeRef.HasValue)
            ? (double?)ExifLngCoordinateToDouble(result2[0], result2[1], result2[2], GpsLongitudeRef.Value)
            : null;



        /// <summary>
        /// GPS latitude reference (North, South).
        /// </summary>
        public ExifTagGpsLatitudeRef? GpsLatitudeRef
        {
            get
            {
                if (Reader.TryGetTagValue<string>(ExifTags.GPSLatitudeRef, out var result) && !string.IsNullOrEmpty(result))
                    return (ExifTagGpsLatitudeRef)result[0];
                return null;
            }
        }

        /// <summary>
        /// GPS longitude reference (East, West).
        /// </summary>
        public ExifTagGpsLongitudeRef? GpsLongitudeRef
        {
            get
            {
                if (Reader.TryGetTagValue<string>(ExifTags.GPSLongitudeRef, out var result) && !string.IsNullOrEmpty(result))
                    return (ExifTagGpsLongitudeRef)result[0];
                return null;
            }
        }

        /// <summary>
        /// Gps altitude in meters
        /// </summary>
        public double? GpsAltitude => Reader.TryGetTagValue<double>(ExifTags.GPSAltitude, out var result) ? (double?)result: null;

        /// <summary>
        /// Indicates the altitude used as the reference altitude. If the reference is sea level and the altitude is above sea level, 0 is given. If the altitude is below sea level, a value of 1 is given and the altitude is indicated as an absolute value in the GSPAltitude tag. The reference unit is meters.
        /// </summary>
        public ExifTagGpsAltitudeRef? GpsAltitudeRef => Reader.TryGetTagValue<ushort>(ExifTags.GPSAltitudeRef, out var result) ? (ExifTagGpsAltitudeRef?)Convert.ToUInt16(result) : null;

        /// <summary>
        /// Gps bearing when the piture was taken
        /// </summary>
        public double? GpsDestBearing => Reader.TryGetTagValue<double>(ExifTags.GPSDestBearing, out var result) ? (double?)result : null;

        /// <summary>
        /// Indicates the reference used for giving the bearing to the destination point. "T" denotes true direction and "M" is magnetic direction.
        /// </summary>
        public ExifTagGpsBearingRef? GpsDestBearingRef
        {
            get
            {
                if (Reader.TryGetTagValue<string>(ExifTags.GPSDestBearingRef, out var result) && !string.IsNullOrEmpty(result))
                    return (ExifTagGpsBearingRef)result[0];
                return null;
            }
        }

        /// <summary>
        /// Indicates the speed of GPS receiver movement, unit is given by the GPSSpeedRef property
        /// </summary>
        public double? GpsSpeed => Reader.TryGetTagValue<double>(ExifTags.GPSSpeed, out var result) ? (double?)result : null;

        /// <summary>
        /// UTC
        /// </summary>
        private string GpsDateStamp => Reader.TryGetTagValue<string>(ExifTags.GPSDateStamp, out var result) ? result : null;
        /// <summary>
        /// UTC
        /// </summary>
        private double[] GpsTimeStamp => Reader.TryGetTagValue<double[]>(ExifTags.GPSTimeStamp, out var result) ? result : null;

        public DateTimeOffset? GpsDateTime
        {
            get
            {
                var date = GpsDateStamp;
                var time = GpsTimeStamp;
                if (date == null || time == null || time.Length != 3 
                    || !DateTimeOffset.TryParseExact(date, "yyyy:MM:dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var datetime))
                    return null;

                return new DateTimeOffset(datetime.Year, datetime.Month, datetime.Day, (int)time[0], (int)time[1], (int)time[2], TimeSpan.Zero);
            }
        }

        /// <summary>
        /// Indicates the unit used to express the GPS receiver speed of movement. "K" "M" and "N" represents kilometers per hour, miles per hour, and knots.
        /// </summary>
        public ExifTagGpsSpeedRef? GpsSpeedRef
        {
            get
            {
                if (Reader.TryGetTagValue<string>(ExifTags.GPSSpeedRef, out var result) && !string.IsNullOrEmpty(result))
                    return (ExifTagGpsSpeedRef) result[0];
                return null;
            }
        }

        #endregion
        #endregion

        
        private double ExifLatCoordinateToDouble(double deg, double min, double sec, ExifTagGpsLatitudeRef coordRef)
        {
            if (coordRef == ExifTagGpsLatitudeRef.North)
                return deg + (min / 60) + (sec / 3600);
            return (deg + (min / 60) + (sec / 3600)) * -1;
        }

        private double ExifLngCoordinateToDouble(double deg, double min, double sec, ExifTagGpsLongitudeRef coordRef)
        {
            if (coordRef == ExifTagGpsLongitudeRef.East)
                return deg + (min / 60) + (sec / 3600);
            return (deg + (min / 60) + (sec / 3600)) * -1;
        }


        /// <summary>
        /// Retreive one exif tag value according to the tag name, this value is not formated, so you have to know the return type on each platform.
        /// For instance latitude is an array of 3 double on Android and  a single double on iOS
        /// </summary>
        /// <typeparam name="T">Type of the result value</typeparam>
        /// <param name="tag">Tag name</param>
        /// <param name="result">Result</param>
        /// <returns>Did succed</returns>
        public bool TryGetRawTagValue<T>(ExifTags tag, out T result)
        {
            return Reader.TryGetTagValue<T>(tag, out result);
        }

        public void Dispose()
        {
            Reader?.Dispose();
            Reader = null;
        }
    }
}
