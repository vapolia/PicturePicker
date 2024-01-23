namespace Vapolia.PicturePicker;

#if ANDROID || IOS
public static partial class AdvancedMediaPicker
{
    public static Task<bool> ChoosePictureFromLibrary(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality=80)
        => PlatformChoosePictureFromLibrary(filePath, saving, maxPixelWidth, maxPixelHeight, percentQuality);

    /// <summary>
    /// Returns null if cancelled
    /// saveToGallery can fails silently
    /// </summary>
    public static Task<bool> TakePicture(string filePath, Action<Task<bool>>? saving = null, int maxPixelWidth=0, int maxPixelHeight=0, int percentQuality=80, bool useFrontCamera=false, bool saveToGallery=false, CancellationToken cancel = default)
        => PlatformTakePicture(filePath, saving, maxPixelWidth, maxPixelHeight, percentQuality, useFrontCamera, saveToGallery, cancel);

    public static bool HasCamera => PlatformHasCamera;
}
#endif
