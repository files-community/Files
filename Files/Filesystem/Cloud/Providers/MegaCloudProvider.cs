using Files.Enums;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Filesystem.Cloud.Providers
{
    public class MegaCloudProvider : ICloudProviderDetector
    {
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            try
            {
                var results = new List<CloudProvider>();

                using var kkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{1ADE4758-6586-42E5-8825-FA9A5FC3D569}");
                System.Diagnostics.Debug.WriteLine((string)kkey?.GetValue(""));
                results.Add(new CloudProvider()
                {
                    ID = CloudProviders.Mega,
                    Name = $"MEGA from reg",
                    SyncFolder = (string)"c:\\"
                });

                using var oneDriveAccountsKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts");
                foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
                {
                    var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
                    var userFolder = (string)Microsoft.Win32.Registry.GetValue(accountKeyName, "UserFolder", null);
                    System.Diagnostics.Debug.WriteLine(userFolder);
                    results.Add(new CloudProvider()
                    {
                        ID = CloudProviders.OneDrive,
                        Name = $"OneDrive from reg",
                        SyncFolder = (string)userFolder
                    });
                }

                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                    {
                        { "Arguments", "DetectMEGASync" }
                    });
                    if (status == AppServiceResponseStatus.Success && response.ContainsKey("Count"))
                    {
                        foreach (var key in response.Keys
                            .Where(k => k != "Count" && k != "RequestID")
                            .OrderBy(o => o))
                        {
                            results.Add(new CloudProvider()
                            {
                                ID = CloudProviders.Mega,
                                Name = $"MEGA ({key})",
                                SyncFolder = (string)response[key]
                            });
                        }

                        return results;
                    }
                }
                return Array.Empty<CloudProvider>();
            }
            catch
            {
                // Not detected
                return Array.Empty<CloudProvider>();
            }
        }
    }
}