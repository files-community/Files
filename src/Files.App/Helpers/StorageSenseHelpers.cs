using System;
using Files.App.Shell;
using Windows.Foundation.Metadata;
using Windows.System;

namespace Files.App.Helpers
{
    internal static class StorageSenseHelpers
    {
        public static async void OpenStorageSense(string path)
        {
            if (!path.StartsWith("C:", StringComparison.OrdinalIgnoreCase)
                && ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                LaunchHelpers.LaunchSettings("page=SettingsPageStorageSenseStorageOverview&target=SystemSettings_StorageSense_VolumeListLink");
            }
            else
            {
                await Launcher.LaunchUriAsync(new Uri("ms-settings:storagesense"));
            }
        }
    }
}
