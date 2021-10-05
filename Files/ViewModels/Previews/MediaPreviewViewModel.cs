using Files.Filesystem;
using Files.ViewModels.Properties;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.UI.Xaml;

namespace Files.ViewModels.Previews
{
    public class MediaPreviewViewModel : BasePreviewModel
    {
        private MediaSource source;

        public MediaPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public static List<string> Extensions => new List<string>() {
            // Video
            ".mp4", ".webm", ".ogg", ".mov", ".qt", ".mp4", ".m4v", ".mp4v", ".3g2", ".3gp2", ".3gp", ".3gpp", ".mkv",
            // Audio
            ".mp3", ".m4a", ".wav", ".wma", ".aac", ".adt", ".adts", ".cda", ".flac"
        };

        public MediaSource Source
        {
            get => source;
            set => SetProperty(ref source, value);
        }

        public override Task<List<FileProperty>> LoadPreviewAndDetails()
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