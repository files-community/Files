using System.Threading.Tasks;
using Files.Backend.Models.Imaging;

#nullable enable

namespace Files.Backend.Services
{
	public interface IImagingService
	{
		Task<ImageModel?> GetImageModelFromDataAsync(byte[]? rawData);
	}
}
