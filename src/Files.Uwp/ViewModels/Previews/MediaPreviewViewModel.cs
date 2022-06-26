using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.UI.Xaml;

namespace Files.Uwp.ViewModels.Previews
{
    public class MediaPreviewViewModel : BasePreviewModel
    {
        public event EventHandler TogglePlaybackRequested;

        private MediaSource source;
        public MediaSource Source
        {
            get => source;
            private set => SetProperty(ref source, value);
        }

        public MediaPreviewViewModel(ListedItem item) : base(item) {}

        public static bool ContainsExtension(string extension) => extension
            is ".mp4" or ".webm" or ".ogg" or ".mov" or ".qt" or ".mp4" or ".m4v" // Video
            or ".mp4v" or ".3g2" or ".3gp2" or ".3gp" or ".3gpp" or ".mkv"        // Video
            or ".mp3" or ".m4a" or ".wav" or ".wma" or ".aac" or ".adt" or ".adts" or ".cda" or ".flac"; // Audio

        public void TogglePlayback() => TogglePlaybackRequested?.Invoke(this, null);

        public override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
        {
            Source = MediaSource.CreateFromStorageFile(Item.ItemFile);
            return Task.FromResult(new List<FileProperty>());
        }

        public override void PreviewControlBase_Unloaded(object sender, RoutedEventArgs e)
        {
            Source = null;
            base.PreviewControlBase_Unloaded(sender, e);
        }
    }
}