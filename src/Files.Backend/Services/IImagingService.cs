using Files.Backend.Models.Imaging;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
    public interface IImagingService
    {
        Task<ImageModel?> GetImageModelFromDataAsync(byte[]? rawData);

        Task<ImageModel?> GetImageModelFromPathAsync(string filePath, uint thumbnailSize = 64u);
    }
}
