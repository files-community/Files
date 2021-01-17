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
        public async Task<IEnumerable<CloudProvider>> DetectAsync()
        {
            try
            {
                var infoPath = @"Dropbox\info.json";
                var jsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPath);
                var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
                var jsonObj = JObject.Parse(await FileIO.ReadTextAsync(configFile));
                var dropboxPath = (string)jsonObj["personal"]["path"];

                return new[] {
                    new CloudProvider()
                    {
                        ID = CloudProviders.DropBox,
                        Name = "Dropbox",
                        SyncFolder = dropboxPath
                    }
                };
            }
            catch
            {
                // Not detected
                return Array.Empty<CloudProvider>();
            }
        }
    }
}