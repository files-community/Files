using Microsoft.Graph;
using System.Threading.Tasks;

namespace Files.Backend.Services.Graph
{
    public interface IGraphRecentFilesService
    {
        Task<IDriveRecentCollectionPage> GetRecentDriveItemsAsync();
    }
}
