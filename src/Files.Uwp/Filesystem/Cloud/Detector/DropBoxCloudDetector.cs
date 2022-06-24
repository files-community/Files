using Files.Shared.Cloud;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;

namespace Files.Uwp.Filesystem.Cloud
{
    public class DropBoxCloudDetector : AbstractCloudDetector
    {
        protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
        {
            string jsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Dropbox\info.json");
            var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
            var jsonObj = JObject.Parse(await FileIO.ReadTextAsync(configFile));

            if (jsonObj.ContainsKey("personal"))
            {
                string dropboxPath = (string)jsonObj["personal"]["path"];

                yield return new CloudProvider(CloudProviders.DropBox)
                {
                    Name = "Dropbox",
                    SyncFolder = dropboxPath,
                };
            }

            if (jsonObj.ContainsKey("business"))
            {
                string dropboxPath = (string)jsonObj["business"]["path"];

                yield return new CloudProvider(CloudProviders.DropBox)
                {
                    Name = "Dropbox Business",
                    SyncFolder = dropboxPath,
                };
            }
        }
    }
}