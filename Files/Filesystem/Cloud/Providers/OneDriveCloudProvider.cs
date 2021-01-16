using Files.Enums;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud.Providers
{
    public class OneDriveCloudProvider : ICloudProviderDetector
    {
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts", false);
                var results = new List<CloudProvider>();

                foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
                {
                    var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
                    var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
                    var userFolder = (string)Registry.GetValue(accountKeyName, "UserFolder", null);

                    if (!string.IsNullOrWhiteSpace(userFolder))
                    {
                        results.Add(new CloudProvider()
                        {
                            ID = CloudProviders.OneDrive,
                            Name = string.IsNullOrWhiteSpace(displayName) ? "OneDrive - Personal" : $"OneDrive - {displayName}",
                            SyncFolder = userFolder
                        });
                    }
                }

                if (results.Count > 0)
                {
                    cloudProviders.AddRange(results
                        .OrderByDescending(o => string.Equals(o.Name, "OneDrive - Personal", System.StringComparison.OrdinalIgnoreCase))
                        .ThenBy(o => o.Name));
                }
            }
            catch
            {
                // Not detected
            }
        }
    }
}