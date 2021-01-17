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
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                var infoPath = @"Dropbox\info.json";
                var jsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPath);
                var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
                var jsonObj = JObject.Parse(await FileIO.ReadTextAsync(configFile));

                if (jsonObj.ContainsKey("personal"))
                {
                    var dropboxPath = (string)jsonObj["personal"]["path"];
                    cloudProviders.Add(new CloudProvider()
                    {
                        ID = CloudProviders.DropBox,
                        Name = "Dropbox",
                        SyncFolder = dropboxPath
                    });
                }

                if (jsonObj.ContainsKey("business"))
                {
                    var dropboxPath = (string)jsonObj["business"]["path"];
                    cloudProviders.Add(new CloudProvider()
                    {
                        ID = CloudProviders.DropBox,
                        Name = "Dropbox Business",
                        SyncFolder = dropboxPath
                    });
                }
            }
            catch
            {
                // Not detected
            }
        }
    }
}