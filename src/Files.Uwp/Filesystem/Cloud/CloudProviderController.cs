﻿using Files.Shared;
using Files.Uwp.Filesystem.Cloud.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Uwp.Filesystem.Cloud
{
    public class CloudProviderController
    {
        private List<ICloudProviderDetector> CloudProviderDetectors => new List<ICloudProviderDetector>
            {
                new GoogleDriveCloudProvider(),
                new DropBoxCloudProvider(),
                new BoxCloudProvider(),
                new AppleCloudProvider(),
                new GenericCloudProvider(),
                new SynologyDriveCloudProvider()
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

            return tasks.SelectMany(o => o.Result).OrderBy(o => o.ID.ToString()).ThenBy(o => o.Name).Distinct().ToList();
        }
    }
}