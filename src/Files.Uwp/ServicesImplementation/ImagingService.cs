using Files.Backend.Models.Imaging;
using Files.Backend.Services;
using Files.Helpers;
using System.Threading.Tasks;

namespace Files.Uwp.ServicesImplementation
{
    internal sealed class ImagingService : IImagingService
    {
        public async Task<ImageModel> GetImageModelFromDataAsync(byte[] rawData)
        {
            return new ReadyBitmapImageModel(await BitmapHelper.ToBitmapAsync(rawData));
        }
    }
}
