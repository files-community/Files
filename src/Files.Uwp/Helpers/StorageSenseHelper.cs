using System;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.System;

namespace Files.Uwp.Helpers
{
    internal class StorageSenseHelper
    {
        public static async void OpenStorageSense(string Path)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null
                && !Path.StartsWith("C:", StringComparison.OrdinalIgnoreCase)
                && ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "LaunchSettings" },
                    { "page", "page=SettingsPageStorageSenseStorageOverview&target=SystemSettings_StorageSense_VolumeListLink" }
                });
            }
            else
            {
                await Launcher.LaunchUriAsync(new Uri("ms-settings:storagesense"));
            }
        }
    }
}
