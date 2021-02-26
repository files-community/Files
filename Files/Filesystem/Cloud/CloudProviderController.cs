using Files.Filesystem.Cloud.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud
{
    public class CloudProviderController
    {
        private List<ICloudProviderDetector> CloudProviderDetectors => new List<ICloudProviderDetector>
            {
                new GoogleDriveCloudProvider(),
                new DropBoxCloudProvider(),
                new OneDriveCloudProvider(),
                new MegaCloudProvider(),
                new BoxCloudProvider(),
                new AppleCloudProvider(),
                new AmazonDriveProvider(),
                new OneDriveSharePointCloudProvider(),
            };

        public async Task<List<CloudProvider>> DetectInstalledCloudProvidersAsync()
        {
            var tasks = new List<Task<IList<CloudProvider>>>();
            var results = new List<CloudProvider>();

            foreach (var provider in CloudProviderDetectors)
            {
                tasks.Add(provider.DetectAsync());
            }

            await Task.WhenAll(tasks);

            return tasks.SelectMany(o => o.Result).Distinct().ToList();
        }
    }
}