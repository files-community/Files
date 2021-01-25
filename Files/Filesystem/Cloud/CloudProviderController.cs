using Files.Filesystem.Cloud.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud
{
    public class CloudProviderController
    {
        private List<CloudProvider> cloudProviders = new List<CloudProvider>();

        public List<ICloudProviderDetector> CloudProviderDetectors => new List<ICloudProviderDetector>
            {
                new GoogleDriveCloudProvider(),
                new DropBoxCloudProvider(),
                new OneDriveCloudProvider(),
                new MegaCloudProvider(),
                new BoxCloudProvider(),
                new AppleCloudProvider(),
                new AmazonDriveProvider()
            };

        public List<CloudProvider> CloudProviders
        {
            get => cloudProviders.Where(x => !string.IsNullOrEmpty(x.SyncFolder)).ToList();
            set => cloudProviders = value;
        }

        public async Task DetectInstalledCloudProvidersAsync()
        {
            var tasks = new List<Task<IList<CloudProvider>>>();
            var results = new List<CloudProvider>();

            foreach (var provider in CloudProviderDetectors)
            {
                tasks.Add(provider.DetectAsync());
            }

            await Task.WhenAll(tasks);

            cloudProviders = tasks.SelectMany(o => o.Result).ToList();
        }
    }
}