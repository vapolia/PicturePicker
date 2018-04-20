//using System;

//namespace Vapolia.Mvvmcross.PicturePicker.Lib
//{
//    public interface IExifReader : IDisposable
//    {
//        /// <summary>
//        /// Try to retreive an Exif tag value by its ushort code, you must supply Type of the result , for a int of the result's type see
//        /// http://www.exiv2.org/tags.html
//        /// </summary>
//        /// <typeparam name="T">.NET Type of the result</typeparam>
//        /// <param name="tagId">Ushort code of the Exif tag</param>
//        /// <param name="result">The result</param>
//        /// <returns>True is successfull false otherwise</returns>
//        bool TryGetTagValue<T>(ushort tagId, out T result);

//        /// <summary>
//        /// Try to retreive an Exif tag value using the <see cref="tag"/> enum, you must supply Type of the result, for a int of the result's type see
//        /// http://www.exiv2.org/tags.html
//        /// </summary>
//        /// <typeparam name="T">.NET Type of the result</typeparam>
//        /// <param name="tag">The Exif tag  name</param>
//        /// <param name="result">The result</param>
//        /// <returns>True is successfull false otherwise</returns>
//        bool TryGetTagValue<T>(ExifTags tag, out T result);

//        /// <summary>
//        /// Add an exif tag in the cached dictionnary but dont write to the underlying file
//        /// </summary>
//        /// <param name="tag"></param>
//        /// <param name="obj"></param>
//        void SetTagValueInCache(ExifTags tag, object obj);
//    }
//}
