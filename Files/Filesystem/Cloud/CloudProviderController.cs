using Files.Filesystem.Cloud.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud
{
    public class CloudProviderController
    {
        private List<CloudProvider> cloudProviders;

        public CloudProviderController()
        {
            CloudProviderDetectors = new List<ICloudProviderDetector>
            {
                new GoogleDriveCloudProvider(),
                new DropBoxCloudProvider(),
                new OneDriveCloudProvider(),
                new MegaCloudProvider(),
                new BoxCloudProvider(),
                new AppleCloudProvider(),
                new AmazonDriveProvider()
            };

            CloudProviders = new List<CloudProvider>();
        }

        public List<ICloudProviderDetector> CloudProviderDetectors { get; set; }

        public List<CloudProvider> CloudProviders
        {
            get => cloudProviders.Where(x => !string.IsNullOrEmpty(x.SyncFolder)).ToList();
            set => cloudProviders = value;
        }

        public async Task DetectInstalledCloudProvidersAsync()
        {
            foreach (var provider in CloudProviderDetectors)
            {
                await provider.DetectAsync(cloudProviders);
            }
        }
    }
}