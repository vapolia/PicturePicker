using Microsoft.Extensions.Logging.Abstractions;

namespace Vapolia.PicturePicker;

public static partial class AdvancedMediaPicker
{
    private static readonly IPicturePicker picturePicker = new PlatformLib.PicturePicker(NullLogger.Instance);

    static Task<bool> PlatformChoosePictureFromLibrary(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality=80)
        => picturePicker.ChoosePictureFromLibrary(filePath, saving, maxPixelWidth, maxPixelHeight, percentQuality);

    /// <summary>
    /// Returns null if cancelled
    /// saveToGallery can fails silently
    /// </summary>
    static Task<bool> PlatformTakePicture(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth = 0, int maxPixelHeight = 0, int percentQuality = 80, bool useFrontCamera = false, bool saveToGallery = false, CancellationToken cancel = default)
        => picturePicker.TakePicture(filePath, saving, maxPixelWidth, maxPixelHeight, percentQuality, useFrontCamera, saveToGallery, cancel);

    static bool PlatformHasCamera 
        => picturePicker.HasCamera;
}