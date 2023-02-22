using Files.App.AppModels;
using Files.App.Helpers;
using Files.Core.Models;
using Files.Core.Services;
using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;

namespace Files.App.ServicesImplementation
{
	internal sealed class ImagingService : IImageService
	{
		/// <inheritdoc/>
		public async Task<IImageModel?> GetIconAsync(IStorable storable, CancellationToken cancellationToken)
		{
			if (storable is not ILocatableStorable locatableStorable)
				return null;

			var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(locatableStorable.Path, 24u, ThumbnailMode.ListView);
			if (iconData is null)
				return null;

			var bitmapImage = await iconData.ToBitmapAsync();
			return new BitmapImageModel(bitmapImage);
		}

		public async Task<IImageModel?> GetImageModelFromDataAsync(byte[] rawData)
		{
			return new BitmapImageModel(await BitmapHelper.ToBitmapAsync(rawData));
		}

		public async Task<IImageModel?> GetImageModelFromPathAsync(string filePath, uint thumbnailSize = 64)
		{
			if (await FileThumbnailHelper.LoadIconFromPathAsync(filePath, thumbnailSize, ThumbnailMode.ListView) is byte[] imageBuffer)
				return await GetImageModelFromDataAsync(imageBuffer);

			return null;
		}
	}
}
