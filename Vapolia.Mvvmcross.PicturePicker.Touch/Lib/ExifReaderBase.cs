using CoreLocation;
using Foundation;
using System;
using System.Collections.Generic;
using System.Drawing;
using Vapolia.Mvvmcross.PicturePicker.Lib;


namespace Vapolia.Mvvmcross.PicturePicker.Touch.Lib
{
    /// <summary>
    /// Base class for iOS Exif Readers, parse meta data in native iOS dictionaries and store data in a single dictionary in order to easy latter search.
    /// Note that all Exif tags are not available on this iOS implementation since each tag must be retreived from
    /// one of the below dictionnary, if you need more tags make a request a github or add your tags in a PR :)
    /// To add a tag you must match the iOS propery name : https://developer.apple.com/library/ios/documentation/GraphicsImaging/Reference/CGImageProperties_Reference/#//apple_ref/doc/constant_group/EXIF_Dictionary_Keys
    /// with the local ExifTags enum an find the right return type using : http://www.exiv2.org/tags.html
    /// </summary>
    public abstract class ExifReaderBase
    {
        protected Dictionary<ExifTags, object> _exifCache;

        protected ExifReaderBase()
        {
            _exifCache = new Dictionary<ExifTags, object>();
        }

        /// <summary>
        /// Gps Data
        /// </summary>
        /// <param name="gpsdico"></param>
        protected void SetGpsData(NSDictionary gpsdico)
        {
            _exifCache.Add(ExifTags.GPSAltitude, (gpsdico[ImageIO.CGImageProperties.GPSAltitude] != null) ? (double?)(NSNumber)gpsdico[ImageIO.CGImageProperties.GPSAltitude] : null);
            _exifCache.Add(ExifTags.GPSAltitudeRef, (gpsdico[ImageIO.CGImageProperties.GPSAltitudeRef] != null) ? (ushort?)(NSNumber)gpsdico[ImageIO.CGImageProperties.GPSAltitudeRef] : null);
            _exifCache.Add(ExifTags.GPSLatitude, (gpsdico[ImageIO.CGImageProperties.GPSLatitude] != null) ? (double?)(NSNumber)gpsdico[ImageIO.CGImageProperties.GPSLatitude] : null);
            _exifCache.Add(ExifTags.GPSLongitude, (gpsdico[ImageIO.CGImageProperties.GPSLongitude] != null) ? (double?)(NSNumber)gpsdico[ImageIO.CGImageProperties.GPSLongitude] : null);
            _exifCache.Add(ExifTags.GPSLongitudeRef, (gpsdico[ImageIO.CGImageProperties.GPSLongitudeRef] != null) ? ((string)(NSString)gpsdico[ImageIO.CGImageProperties.GPSLongitudeRef]) : null);
            _exifCache.Add(ExifTags.GPSLatitudeRef, (gpsdico[ImageIO.CGImageProperties.GPSLatitudeRef] != null) ? ((string)(NSString)gpsdico[ImageIO.CGImageProperties.GPSLatitudeRef]) : null);
            _exifCache.Add(ExifTags.GPSDestBearing, (gpsdico[ImageIO.CGImageProperties.GPSDestBearing] != null) ? (double?)(NSNumber)gpsdico[ImageIO.CGImageProperties.GPSDestBearing] : null);
            _exifCache.Add(ExifTags.GPSDestBearingRef, (gpsdico[ImageIO.CGImageProperties.GPSDestBearingRef] != null) ? ((string)(NSString)gpsdico[ImageIO.CGImageProperties.GPSDestBearingRef]) : null);
            _exifCache.Add(ExifTags.GPSSpeed, (gpsdico[ImageIO.CGImageProperties.GPSSpeed] != null) ? (double?)(NSNumber)gpsdico[ImageIO.CGImageProperties.GPSSpeed] : null);
            _exifCache.Add(ExifTags.GPSSpeedRef, (gpsdico[ImageIO.CGImageProperties.GPSSpeedRef] != null) ? ((string)(NSString)gpsdico[ImageIO.CGImageProperties.GPSSpeedRef]) : null);

        }

        protected void SetGpsData(CLLocation loc)
        {
            _exifCache.Add(ExifTags.GPSAltitude, loc.Altitude); //meters
            _exifCache.Add(ExifTags.GPSAltitudeRef, ImageIO.CGImageProperties.GPSAltitudeRef);
            _exifCache.Add(ExifTags.GPSLatitude, loc.Coordinate.Latitude);
            _exifCache.Add(ExifTags.GPSLongitude, loc.Coordinate.Longitude);
            //_exifCache.Add(ExifTags.GPSLongitudeRef, (gpsdico[ImageIO.CGImageProperties.GPSLongitudeRef] != null) ? ((string)gpsdico[ImageIO.CGImageProperties.GPSLongitudeRef]) : null);
            //_exifCache.Add(ExifTags.GPSLatitudeRef, (gpsdico[ImageIO.CGImageProperties.GPSLatitudeRef] != null) ? ((string)gpsdico[ImageIO.CGImageProperties.GPSLatitudeRef]) : null);
            _exifCache.Add(ExifTags.GPSDestBearing, loc.Course<0? (double?)null : loc.Course);
           // _exifCache.Add(ExifTags.GPSDestBearingRef, (gpsdico[ImageIO.CGImageProperties.GPSDestBearingRef] != null) ? ((string)gpsdico[ImageIO.CGImageProperties.GPSDestBearingRef]) : null);
            _exifCache.Add(ExifTags.GPSSpeed, loc.Speed < 0 ? (double?)null : loc.Speed); //meter per sec
            //_exifCache.Add(ExifTags.GPSSpeedRef, (gpsdico[ImageIO.CGImageProperties.GPSSpeedRef] != null) ? ((string)gpsdico[ImageIO.CGImageProperties.GPSSpeedRef]) : null);

        }

        /// <summary>
        /// Tiff data
        /// </summary>
        /// <param name="tiffdico"></param>
        protected void SetTiffData(NSDictionary tiffdico)
        {
            _exifCache.Add(ExifTags.YResolution, (tiffdico[ImageIO.CGImageProperties.TIFFYResolution] != null) ? (double?)(NSNumber)tiffdico[ImageIO.CGImageProperties.TIFFYResolution] : null);
            _exifCache.Add(ExifTags.XResolution, (tiffdico[ImageIO.CGImageProperties.TIFFXResolution] != null) ? (double?)(NSNumber)tiffdico[ImageIO.CGImageProperties.TIFFXResolution] : null);
            _exifCache.Add(ExifTags.Model, (tiffdico[ImageIO.CGImageProperties.TIFFModel] != null) ? (string)(NSString)tiffdico[ImageIO.CGImageProperties.TIFFModel] : null);
            _exifCache.Add(ExifTags.Make, (tiffdico[ImageIO.CGImageProperties.TIFFMake] != null) ? (string)(NSString)tiffdico[ImageIO.CGImageProperties.TIFFMake] : null);
            _exifCache.Add(ExifTags.ResolutionUnit, (tiffdico[ImageIO.CGImageProperties.TIFFResolutionUnit] != null) ? (ExifTagResolutionUnit?)(int)(NSNumber)tiffdico[ImageIO.CGImageProperties.TIFFResolutionUnit] : null);
            _exifCache.Add(ExifTags.Software, (string)(NSString)tiffdico[ImageIO.CGImageProperties.TIFFSoftware]);
            _exifCache.Add(ExifTags.DateTime, (string)(NSString)tiffdico[ImageIO.CGImageProperties.TIFFDateTime]);
            _exifCache.Add(ExifTags.ImageDescription, (string)(NSString)tiffdico[ImageIO.CGImageProperties.TIFFImageDescription]);
            _exifCache.Add(ExifTags.Artist, (string)(NSString)tiffdico[ImageIO.CGImageProperties.TIFFArtist]);
            _exifCache.Add(ExifTags.Copyright, (string)(NSString)tiffdico[ImageIO.CGImageProperties.PNGCopyright]);
            _exifCache.Add(ExifTags.Compression, (tiffdico[ImageIO.CGImageProperties.TIFFCompression] != null) ? (ExifTagCompression?)(int)(NSNumber)tiffdico[ImageIO.CGImageProperties.TIFFCompression] : null);


        }
        /// <summary>
        /// Global data
        /// </summary>
        /// <param name="metadata"></param>
        protected void SetGlobalData(NSDictionary metadata)
        {
            _exifCache.Add(ExifTags.Orientation, (metadata[ImageIO.CGImageProperties.Orientation] != null) ? (int?)(NSNumber)metadata[ImageIO.CGImageProperties.Orientation] : null);
 
        }

        protected void SetExifData(NSDictionary exifdico)
        {
            _exifCache.Add(ExifTags.Flash, (exifdico[ImageIO.CGImageProperties.ExifFlash] != null) ? (ExifTagFlash?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifFlash] : null);
            _exifCache.Add(ExifTags.ExposureTime, (exifdico[ImageIO.CGImageProperties.ExifExposureTime] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifExposureTime] : null);
            _exifCache.Add(ExifTags.FNumber, (exifdico[ImageIO.CGImageProperties.ExifFNumber] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifFNumber] : null);
            _exifCache.Add(ExifTags.ExposureProgram, (exifdico[ImageIO.CGImageProperties.ExifExposureProgram] != null) ? (ExifTagExposureProgram?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifExposureProgram] : null);
            _exifCache.Add(ExifTags.SpectralSensitivity, (exifdico[ImageIO.CGImageProperties.ExifSpectralSensitivity] != null) ? (string)(NSString)exifdico[ImageIO.CGImageProperties.ExifSpectralSensitivity] : null);
            _exifCache.Add(ExifTags.ExifVersion, (exifdico[ImageIO.CGImageProperties.ExifVersion] != null) ? ((NSArray)exifdico[ImageIO.CGImageProperties.ExifVersion]).ToList<int, NSNumber>() : null);
            _exifCache.Add(ExifTags.ISOSpeedRatings, (exifdico[ImageIO.CGImageProperties.ExifISOSpeedRatings] != null) ? ((NSArray)exifdico[ImageIO.CGImageProperties.ExifISOSpeedRatings]).ToList<int, NSNumber>() : null);
            _exifCache.Add(ExifTags.DateTimeOriginal, (exifdico[ImageIO.CGImageProperties.ExifDateTimeOriginal] != null) ? (string)(NSString)exifdico[ImageIO.CGImageProperties.ExifDateTimeOriginal] : null);
            _exifCache.Add(ExifTags.ColorSpace, (exifdico[ImageIO.CGImageProperties.ExifColorSpace] != null) ? (int?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifColorSpace] : null);
            _exifCache.Add(ExifTags.DateTimeDigitized, (exifdico[ImageIO.CGImageProperties.ExifDateTimeDigitized] != null) ? (string)(NSString)exifdico[ImageIO.CGImageProperties.ExifDateTimeDigitized] : null);
            _exifCache.Add(ExifTags.OECF, (exifdico[ImageIO.CGImageProperties.ExifOECF] != null) ? (string)(NSString)exifdico[ImageIO.CGImageProperties.ExifOECF] : null);

            _exifCache.Add(ExifTags.ComponentsConfiguration, (exifdico[ImageIO.CGImageProperties.ExifComponentsConfiguration] != null) ? ((NSArray)exifdico[ImageIO.CGImageProperties.ExifComponentsConfiguration]).ToList<int, NSNumber>() : null);
            _exifCache.Add(ExifTags.ShutterSpeedValue, (exifdico[ImageIO.CGImageProperties.ExifShutterSpeedValue] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifShutterSpeedValue] : null);

            _exifCache.Add(ExifTags.ApertureValue, (exifdico[ImageIO.CGImageProperties.ExifApertureValue] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifApertureValue] : null);
            _exifCache.Add(ExifTags.BrightnessValue, (exifdico[ImageIO.CGImageProperties.ExifBrightnessValue] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifBrightnessValue] : null);
            _exifCache.Add(ExifTags.UserComment, (exifdico[ImageIO.CGImageProperties.ExifUserComment] != null) ? (string)(NSString)exifdico[ImageIO.CGImageProperties.ExifUserComment] : null);

            _exifCache.Add(ExifTags.CompressedBitsPerPixel, (exifdico[ImageIO.CGImageProperties.ExifCompressedBitsPerPixel] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifCompressedBitsPerPixel] : null);
            _exifCache.Add(ExifTags.ExposureBiasValue, (exifdico[ImageIO.CGImageProperties.ExifExposureBiasValue] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifExposureBiasValue] : null);
            _exifCache.Add(ExifTags.MaxApertureValue, (exifdico[ImageIO.CGImageProperties.ExifMaxApertureValue] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifMaxApertureValue] : null);
            _exifCache.Add(ExifTags.SubjectDistance, (exifdico[ImageIO.CGImageProperties.ExifSubjectDistance] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifSubjectDistance] : null);

            _exifCache.Add(ExifTags.MeteringMode, (exifdico[ImageIO.CGImageProperties.ExifMeteringMode] != null) ? (ExifTagMeteringMode?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifMeteringMode] : null);
            _exifCache.Add(ExifTags.LightSource, (exifdico[ImageIO.CGImageProperties.ExifLightSource] != null) ? (ExifTagLightSource?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifLightSource] : null);
            _exifCache.Add(ExifTags.FocalLength, (exifdico[ImageIO.CGImageProperties.ExifFocalLength] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifFocalLength] : null);
            _exifCache.Add(ExifTags.FlashEnergy, (exifdico[ImageIO.CGImageProperties.ExifFlashEnergy] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifFlashEnergy] : null);

            _exifCache.Add(ExifTags.FocalPlaneXResolution, (exifdico[ImageIO.CGImageProperties.ExifFocalPlaneXResolution] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifFocalPlaneXResolution] : null);
            _exifCache.Add(ExifTags.FocalPlaneYResolution, (exifdico[ImageIO.CGImageProperties.ExifFocalPlaneYResolution] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifFocalPlaneYResolution] : null);
            //Should be an enum for that
            _exifCache.Add(ExifTags.FocalPlaneResolutionUnit, (exifdico[ImageIO.CGImageProperties.ExifFocalPlaneResolutionUnit] != null) ? (int?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifFocalPlaneResolutionUnit] : null);

            _exifCache.Add(ExifTags.SubjectLocation, (exifdico[ImageIO.CGImageProperties.ExifSubjectLocation] != null) ? (int?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifSubjectLocation] : null);
            _exifCache.Add(ExifTags.ExposureIndex, (exifdico[ImageIO.CGImageProperties.ExifExposureIndex] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifExposureIndex] : null);
            _exifCache.Add(ExifTags.SensingMethod, (exifdico[ImageIO.CGImageProperties.ExifSensingMethod] != null) ? (ExifTagSensingMethod?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifSensingMethod] : null);
            _exifCache.Add(ExifTags.FlashpixVersion, (exifdico[ImageIO.CGImageProperties.ExifFlashPixVersion] != null) ? ((NSArray)exifdico[ImageIO.CGImageProperties.ExifFlashPixVersion]).ToList<int, NSNumber>() : null);
            _exifCache.Add(ExifTags.PixelXDimension, (exifdico[ImageIO.CGImageProperties.ExifPixelXDimension] != null) ? (int?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifPixelXDimension] : null);
            _exifCache.Add(ExifTags.PixelYDimension, (exifdico[ImageIO.CGImageProperties.ExifPixelYDimension] != null) ? (int?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifPixelYDimension] : null);
            _exifCache.Add(ExifTags.RelatedSoundFile, (exifdico[ImageIO.CGImageProperties.ExifRelatedSoundFile] != null) ? (string)(NSString)exifdico[ImageIO.CGImageProperties.ExifRelatedSoundFile] : null);
            _exifCache.Add(ExifTags.FileSource, (exifdico[ImageIO.CGImageProperties.ExifFileSource] != null) ? (ExifTagFileSource?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifFileSource] : null);
            //Array ?
            _exifCache.Add(ExifTags.CFAPattern, (exifdico[ImageIO.CGImageProperties.ExifCFAPattern] != null) ? (int?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifCFAPattern] : null);
            _exifCache.Add(ExifTags.CustomRendered, (exifdico[ImageIO.CGImageProperties.ExifCustomRendered] != null) ? (ExifTagCustomRendered?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifCustomRendered] : null);
            _exifCache.Add(ExifTags.ExposureMode, (exifdico[ImageIO.CGImageProperties.ExifExposureMode] != null) ? (ExifTagExposureMode?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifExposureMode] : null);
            _exifCache.Add(ExifTags.WhiteBalance, (exifdico[ImageIO.CGImageProperties.ExifWhiteBalance] != null) ? (ExifTagWhiteBalance?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifWhiteBalance] : null);
            _exifCache.Add(ExifTags.DigitalZoomRatio, (exifdico[ImageIO.CGImageProperties.ExifDigitalZoomRatio] != null) ? (double?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifDigitalZoomRatio] : null);
            _exifCache.Add(ExifTags.FocalLengthIn35mmFilm, (exifdico[ImageIO.CGImageProperties.ExifFocalLenIn35mmFilm] != null) ? (int?)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifFocalLenIn35mmFilm] : null);
            _exifCache.Add(ExifTags.SceneCaptureType, (exifdico[ImageIO.CGImageProperties.ExifSceneCaptureType] != null) ? (ExifTagSceneCaptureType?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifSceneCaptureType] : null);
            _exifCache.Add(ExifTags.GainControl, (exifdico[ImageIO.CGImageProperties.ExifGainControl] != null) ? (ExifTagGainControl?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifGainControl] : null);

            _exifCache.Add(ExifTags.SceneType, (exifdico[ImageIO.CGImageProperties.ExifSceneType] != null) ? (ExifTagSceneType?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifSceneType] : null);


            _exifCache.Add(ExifTags.Contrast, (exifdico[ImageIO.CGImageProperties.ExifContrast] != null) ? (ExifTagContrast?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifContrast] : null);
            _exifCache.Add(ExifTags.Saturation, (exifdico[ImageIO.CGImageProperties.ExifSaturation] != null) ? (ExifTagSaturation?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifSaturation] : null);
            _exifCache.Add(ExifTags.Sharpness, (exifdico[ImageIO.CGImageProperties.ExifSharpness] != null) ? (ExifTagSharpness?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifSharpness] : null);
            _exifCache.Add(ExifTags.DeviceSettingDescription, (exifdico[ImageIO.CGImageProperties.ExifDeviceSettingDescription] != null) ? (string)(NSString)exifdico[ImageIO.CGImageProperties.ExifDeviceSettingDescription] : null);
            _exifCache.Add(ExifTags.SubjectDistanceRange, (exifdico[ImageIO.CGImageProperties.ExifSubjectDistRange] != null) ? (ExifTagSubjectDistanceRange?)(int)(NSNumber)exifdico[ImageIO.CGImageProperties.ExifSubjectDistRange] : null);
            _exifCache.Add(ExifTags.ImageUniqueID, (exifdico[ImageIO.CGImageProperties.ExifImageUniqueID] != null) ? (string)(NSString)exifdico[ImageIO.CGImageProperties.ExifImageUniqueID] : null);
        }

        public bool TryGetTagValue<T>(ushort tagId, out T result)
        {
            if (_exifCache.ContainsKey((ExifTags)tagId))
            {
                try
                {
                    result = (T)_exifCache[(ExifTags)tagId];
                    return true;
                }
                catch
                {
                    //fallback
                }
            }

            //For image that dont have any metadata
            result = default(T);
            return false;
        }

        public bool TryGetTagValue<T>(ExifTags tag, out T result)
        {
            return TryGetTagValue((ushort)tag, out result);
        }

        /// <summary>
        /// Set an Exif Tag in the cache only (dont write it to the file)
        /// </summary>
        public void SetTagValueInCache(ExifTags tag, object obj)
        {
            if(_exifCache.ContainsKey(tag))
                _exifCache[tag] = obj;
            else
                _exifCache.Add(tag, obj);
        }
    }

    internal static class Extensions
    {
        public static List<T> ToList<T,TInArray>(this NSArray nsArray) where TInArray : NSObject
        {
            var array = NSArray.FromArray<TInArray>(nsArray);

            var n = array.Length;
            var list = new List<T>(n);
            for (var i = 0; i < n; i++)
                list.Add((T)ToObject<T>(array[i]));

            return list;
        }

        public static object ToObject<TTarget>(this NSObject ns)
        {
            if (ns == null)
                return null;

            if (ns is NSString s)
                return (string)s;

            if (ns is NSDate nsDate)
                return (DateTime)nsDate;

            if (ns is NSDecimalNumber dec)
                return (decimal) dec.NSDecimalValue;

            var targetType = typeof(TTarget);

            if (ns is NSNumber x)
            {
                switch (Type.GetTypeCode(targetType))
                {
                    case TypeCode.Boolean:
                        return x.BoolValue;
                    case TypeCode.Char:
                        return (char)x.ByteValue;
                    case TypeCode.SByte:
                        return x.SByteValue;
                    case TypeCode.Byte:
                        return x.ByteValue;
                    case TypeCode.Int16:
                        return x.Int16Value;
                    case TypeCode.UInt16:
                        return x.UInt16Value;
                    case TypeCode.Int32:
                        return x.Int32Value;
                    case TypeCode.UInt32:
                        return x.UInt32Value;
                    case TypeCode.Int64:
                        return x.Int64Value;
                    case TypeCode.UInt64:
                        return x.UInt64Value;
                    case TypeCode.Single:
                        return x.FloatValue;
                    case TypeCode.Double:
                        return x.DoubleValue;
                }
            }

            if (ns is NSValue v)
            {
                if (targetType == typeof(IntPtr))
                    return v.PointerValue;

                if (targetType == typeof(SizeF))
                    return v.SizeFValue;

                if (targetType == typeof(RectangleF))
                    return v.RectangleFValue;

                if (targetType == typeof(PointF))
                    return v.PointFValue;
            }

            throw new NotSupportedException($"Don't know how to convert NSObject to {targetType}");
            //return ns;
        }
    }

}
