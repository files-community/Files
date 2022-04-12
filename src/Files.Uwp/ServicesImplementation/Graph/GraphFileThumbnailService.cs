using CommunityToolkit.Graph.Extensions;
using Files.Backend.Services.Graph;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Uwp.ServicesImplementation.Graph
{
    internal sealed class GraphFileThumbnailService : IGraphFileThumbnailService
    {
        public async Task<Stream> GetDriveItemThumbnailAsync(string id)
        {
            var graphClient = App.GraphAuthenticationProvider.GetClient();

            var item = await graphClient.Me.Drive.Items[id].Thumbnails["0"]["small"].Content.Request().GetAsync();

            return item;
        }
    }
}
