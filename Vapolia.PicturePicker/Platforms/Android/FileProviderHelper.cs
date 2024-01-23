namespace Vapolia.PicturePicker;

internal static class FileProviderHelper
{
    public static FileProviderLocation TemporaryLocation { get; set; } = FileProviderLocation.PreferExternal;

    public static Java.IO.File GetTemporaryDirectory()
    {
        var root = GetTemporaryRootDirectory();
        var dir = new Java.IO.File(root, Guid.NewGuid().ToString("N"));
        dir.Mkdirs();
        dir.DeleteOnExit();
        return dir;
    }

    public static Java.IO.File GetTemporaryRootDirectory()
    {
        // If we specifically want the internal storage, no extra checks are needed, we have permission
        if (TemporaryLocation == FileProviderLocation.Internal)
            return Platform.AppContext.CacheDir;

        // If we explicitly want only external locations we need to do some permissions checking
        var externalOnly = TemporaryLocation == FileProviderLocation.External;

        // make sure the external storage is available
        var hasExternalMedia = Android.OS.Environment.GetExternalStorageState(Platform.AppContext.ExternalCacheDir) == Android.OS.Environment.MediaMounted;

        // fail if we need the external storage, but there is none
        if (externalOnly && !hasExternalMedia)
            throw new InvalidOperationException("Unable to access the external storage, the media is not mounted.");

        // based on permssions, return the correct directory
        // if permission were required, then it would have already thrown
        return hasExternalMedia ? Platform.AppContext.ExternalCacheDir : Platform.AppContext.CacheDir;
    }
}