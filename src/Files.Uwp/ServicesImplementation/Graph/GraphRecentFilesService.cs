using CommunityToolkit.Authentication;
using CommunityToolkit.Graph.Extensions;
using Files.Backend.Services.Graph;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Uwp.ServicesImplementation.Graph
{
    internal sealed class GraphRecentFilesService : IGraphRecentFilesService
    {
        public async Task<IDriveRecentCollectionPage> GetRecentDriveItemsAsync()
        {
            ProviderManager.Instance.GlobalProvider = new WindowsProvider(new string[] { "User.Read" });
            IProvider provider = ProviderManager.Instance.GlobalProvider;
            if (provider?.State == ProviderState.SignedIn)
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
