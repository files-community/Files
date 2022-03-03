using Files.Backend.Models.Imaging;
using System.Threading.Tasks;

#nullable enable

namespace Files.Backend.Services
{
    public interface IImagingService
    {
        Task<ImageModel?> GetImageModelFromDataAsync(byte[]? rawData);
    }
}
