using Files.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.Cloud.Providers
{
    public class DropBoxCloudProvider : ICloudProviderDetector
    {
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            try
            {
                var infoPath = @"Dropbox\info.json";
                var jsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPath);
                var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
                var jsonObj = JObject.Parse(await FileIO.ReadTextAsync(configFile));
                var results = new List<CloudProvider>();

                if (jsonObj.ContainsKey("personal"))
                {
                    var dropboxPath = (string)jsonObj["personal"]["path"];
                    results.Add(new CloudProvider()
                    {
                        ID = CloudProviders.DropBox,
                        Name = "Dropbox",
                        SyncFolder = dropboxPath
                    });
                }

                if (jsonObj.ContainsKey("business"))
                {
                    var dropboxPath = (string)jsonObj["business"]["path"];
                    results.Add(new CloudProvider()
                    {
                        ID = CloudProviders.DropBox,
                        Name = "Dropbox Business",
                        SyncFolder = dropboxPath
                    });
                }

                return results;
            }
            catch
            {
                // Not detected
                return Array.Empty<CloudProvider>();
            }
        }
    }
}