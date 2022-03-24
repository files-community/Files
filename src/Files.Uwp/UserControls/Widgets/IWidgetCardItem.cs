using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.UserControls.Widgets
{
    public interface IWidgetCardItem<T>
    {
        public T Item { get; }

        public bool HasThumbnail { get; }

        public BitmapImage Thumbnail { get; }

        public Task LoadCardThumbnailAsync(int overrideThumbnailSize = 32);
    }
}
