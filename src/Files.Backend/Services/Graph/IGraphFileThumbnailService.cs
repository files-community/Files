using System.IO;
using System.Threading.Tasks;

namespace Files.Backend.Services.Graph
{
    public interface IGraphFileThumbnailService
    {
        Task<Stream> GetDriveItemThumbnailAsync(string id);
    }
}
