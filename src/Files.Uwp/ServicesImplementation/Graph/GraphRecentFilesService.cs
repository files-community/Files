using CommunityToolkit.Authentication;
using CommunityToolkit.Graph.Extensions;
using Files.Backend.Services.Graph;
using Microsoft.Graph;
using System.Threading.Tasks;

namespace Files.Uwp.ServicesImplementation.Graph
{
    internal sealed class GraphRecentFilesService : IGraphRecentFilesService
    {
        public async Task<IDriveRecentCollectionPage> GetRecentDriveItemsAsync()
        {
            if (App.GraphAuthenticationProvider is IProvider provider && provider.State == ProviderState.SignedIn)
            {
                var graphClient = provider.GetClient();

                return await graphClient.Me.Drive
                .Recent()
                .Request()
                .GetAsync();
            }
            else
            {
                return null;
            }
        }
    }
}
