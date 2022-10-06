using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;

namespace Files.App.UserControls.Widgets
{
	public interface IWidgetCardItem<T>
	{
		T Item { get; }

		bool HasThumbnail { get; }

		BitmapImage Thumbnail { get; }

		Task LoadCardThumbnailAsync();
	}
}