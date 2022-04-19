using System.Threading.Tasks;

namespace Files.Uwp.UserControls.Widgets
{
    public interface IWidgetCardItem<T>
    {
        T Item { get; }

        bool HasThumbnail { get; }

        Task LoadCardThumbnailAsync(int overrideThumbnailSize = 32);
    }
}