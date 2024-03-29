﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vapolia.PicturePicker
{
    public interface IPicturePicker
    {
        Task<bool> ChoosePictureFromLibrary(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality=80);

        /// <summary>
        /// Returns null if cancelled
        /// saveToGallery can fails silently
        /// </summary>
        Task<bool> TakePicture(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality=80, bool useFrontCamera=false, bool saveToGallery=false, CancellationToken cancel = default);

        bool HasCamera { get; }
    }
}
