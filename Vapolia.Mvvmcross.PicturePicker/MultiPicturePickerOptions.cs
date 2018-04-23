using System;
using System.Threading;

namespace Vapolia.Mvvmcross.PicturePicker
{
    public class MultiPicturePickerOptions
    {
        /// <summary>
        /// Callback to display a waiting indicator while images are saving
        /// </summary>
        public Action<CancellationToken> SavingAction { get; set; }

        public string Title { get; set; }
        public string NavigationBarPrompt { get; set; }
        public string DoneButtonTitle { get; set; }
        public string CancelButtonTitle { get; set; }
        public string PhotosAccessDeniedErrorTitle { get; set; }

        /// <summary>
        /// Max image width or 0
        /// </summary>
        public int MaxPixelWidth { get;set; }
        /// <summary>
        /// Max image height or 0
        /// </summary>
        public int MaxPixelHeight { get; set; }
        /// <summary>
        /// JPG quality (1-100)
        /// </summary>
        public int PercentQuality { get; set; } = 80;
    }
}