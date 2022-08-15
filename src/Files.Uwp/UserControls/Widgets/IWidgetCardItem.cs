using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.Uwp.UserControls.Widgets
{
    public interface IWidgetCardItem<T>
    {
        T Item { get; }

        bool HasThumbnail { get; }

        BitmapImage Thumbnail { get; }

        Task LoadCardThumbnailAsync(int overrideThumbnailSize = 32);
    }
}