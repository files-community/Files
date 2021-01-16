using Files.Enums;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud.Providers
{
    public class OneDriveCloudProvider : ICloudProviderDetector
    {
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts", false);

                        foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
                        {
                            var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
                            var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
                            var userFolder = (string)Registry.GetValue(accountKeyName, "UserFolder", null);

                            if (!string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(userFolder))
                            {
                                cloudProviders.Add(new CloudProvider()
                                {
                                    ID = CloudProviders.OneDrive,
                                    Name = $"OneDrive - {displayName}",
                                    SyncFolder = userFolder
                                });
                            }
                        }
                    }
                    catch (SecurityException) { }
                });
            }
            catch
            {
                // Not detected
            }
        }
    }
}