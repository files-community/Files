using Files.App.Helpers;
using Files.App.Imaging;
using Files.Backend.Models.Imaging;
using Files.Backend.Services;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;

namespace Files.App.ServicesImplementation
{
	internal sealed class ImagingService : IImagingService
	{
		public async Task<ImageModel?> GetImageModelFromDataAsync(byte[] rawData)
		{
			return new BitmapImageModel(await BitmapHelper.ToBitmapAsync(rawData));
		}

		public async Task<ImageModel?> GetImageModelFromPathAsync(string filePath, uint thumbnailSize = 64)
		{
			ImageModel? imageModel = null;

			if (await FileThumbnailHelper.LoadIconFromPathAsync(filePath, thumbnailSize, ThumbnailMode.ListView) is byte[] imageBuffer)
			{
				imageModel = await GetImageModelFromDataAsync(imageBuffer);
			}

			return imageModel;
		}
	}
}
