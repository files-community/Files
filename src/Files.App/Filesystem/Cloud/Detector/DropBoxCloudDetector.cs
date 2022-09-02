using Files.Shared.Cloud;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Vanara.Extensions.Reflection;
using Windows.Storage;

namespace Files.App.Filesystem.Cloud
{
    public class DropBoxCloudDetector : AbstractCloudDetector
    {
        protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
        {
            string jsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Dropbox\info.json");
            var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
            var jsonDoc = JsonDocument.Parse(await FileIO.ReadTextAsync(configFile));
            var jsonElem = jsonDoc.RootElement;

            if (jsonElem.TryGetProperty("personal", out JsonElement inner))
            {
                string dropboxPath = inner.GetFieldValue<string>("path");

                yield return new CloudProvider(CloudProviders.DropBox)
                {
                    Name = "Dropbox",
                    SyncFolder = dropboxPath,
                };
            }

            if (jsonElem.TryGetProperty("business", out JsonElement innerBusiness))
            {
                string dropboxPath = innerBusiness.GetFieldValue<string>("path");

                yield return new CloudProvider(CloudProviders.DropBox)
                {
                    Name = "Dropbox Business",
                    SyncFolder = dropboxPath,
                };
            }

            jsonDoc.Dispose();
        }
    }
}