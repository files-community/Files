using CommunityToolkit.WinUI;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Files.App.ViewModels.Previews
{
	public class ImagePreviewViewModel : BasePreviewModel
	{
		private ImageSource imageSource;
		public ImageSource ImageSource
		{
			get => imageSource;
			private set => SetProperty(ref imageSource, value);
		}

		public ImagePreviewViewModel(ListedItem item) : base(item) { }

		public static bool ContainsExtension(string extension)
			=> extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tiff" or ".ico" or ".webp";

		public override async Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			using IRandomAccessStream stream = await Item.ItemFile.OpenAsync(FileAccessMode.Read);
			await App.Window.DispatcherQueue.EnqueueAsync(async () =>
			{
				BitmapImage bitmap = new();
				await bitmap.SetSourceAsync(stream);
				ImageSource = bitmap;
			});

			return new List<FileProperty>();
		}
	}
}