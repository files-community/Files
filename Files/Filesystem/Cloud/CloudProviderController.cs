using Files.Filesystem.Cloud.Providers;
using System;
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
                new AppleCloudProvider()
            };

            CloudProviders = new List<CloudProvider>();
        }

        public List<ICloudProviderDetector> CloudProviderDetectors { get; set; }

        public List<CloudProvider> CloudProviders
        {
            get => cloudProviders.Where(x => !string.IsNullOrEmpty(x.SyncFolder)).ToList();
            set => cloudProviders = value;
        }

        public void DetectInstalledCloudProviders()
        {
            var tasks = new List<Task<IEnumerable<CloudProvider>>>();

            foreach (var provider in CloudProviderDetectors)
            {
                provider.DetectAsync().ContinueWith(taskResult =>
                {
                    CloudProviderUpdated?.Invoke(this, taskResult.Result);
                });
            }
        }

        public event EventHandler<IEnumerable<CloudProvider>> CloudProviderUpdated;
    }
}