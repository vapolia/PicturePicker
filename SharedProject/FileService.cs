using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross.Platform;
using MvvmCross.Platform.Exceptions;

namespace Vapolia.Mvvmcross.PicturePicker
{
    internal sealed class FileService
    {
        public string BasePath { get; set; }
        public bool AppendDefaultPath { get; set; } = true;

        public static FileService Instance { get; } = new FileService();

        private FileService()
        {
#if __ANDROID__
            BasePath = Android.App.Application.Context.FilesDir.Path;
#else
            BasePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif
        }


        public void EnsureFolderExists(string folderPath)
        {
            var fullPath = FullPath(folderPath);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }

        public bool Exists(string path)
        {
            var fullPath = FullPath(path);
            return File.Exists(fullPath);
        }

        public Task WriteFileAsync(string name, byte[] bytes, CancellationToken cancel)
        {
            return null;
        }

        public void DeleteFolder(string folderPath, bool recursive)
        {
            var fullPath = FullPath(folderPath);
            Directory.Delete(fullPath, recursive);
        }

        public async Task WriteFileAsync(string path, Stream stream, CancellationToken none)
        {
            var fullPath = FullPath(path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            using (var fileStream = File.OpenWrite(fullPath))
                await stream.CopyToAsync(fileStream);
        }

        public void DeleteFile(string filePath)
        {
            var fullPath = FullPath(filePath);
            File.Delete(fullPath);
        }

        public void DeleteFiles(string searchPattern)
        {
            var files = Directory.GetFiles(BasePath, searchPattern, SearchOption.TopDirectoryOnly);
            foreach (var file in files)
                File.Delete(file);
        }

        public bool TryCopy(string from, string to, bool overwrite)
        {
            try
            {
                var fullFrom = FullPath(from);
                var fullTo = FullPath(to);

                if (!System.IO.File.Exists(fullFrom))
                {
                    Mvx.Error("Error during file copy {0} : {1}. File does not exist!", from, to);
                    return false;
                }

                System.IO.File.Copy(fullFrom, fullTo, overwrite);
                return true;
            }
            catch (Exception exception)
            {
                Mvx.Error("Error during file copy {0} : {1} : {2}", from, to, exception.ToLongString());
                return false;
            }
        }

        public string NativePath(string path)
        {
            return FullPath(path);
        }

        private string FullPath(string path)
        {
            if (!AppendDefaultPath)
                return path;
            return AppendPath(path);
        }

        public string AppendPath(string path) => Path.Combine(BasePath, path);
    }
}
