using Files.Common;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FilesFullTrust.Helpers
{
    public class CloudDrivesDetector
    {
        public static async Task<List<CloudProvider>> DetectCloudDrives()
        {
            var tasks = new List<Task<List<CloudProvider>>>()
            {
                Extensions.IgnoreExceptions(DetectOneDrive, Program.Logger),
                Extensions.IgnoreExceptions(DetectSharepoint, Program.Logger),
                Extensions.IgnoreExceptions(DetectGenericCloudDrive, Program.Logger)
            };
            
            await Task.WhenAll(tasks);

            return tasks.Where(o => o.Result != null).SelectMany(o => o.Result).OrderBy(o => o.ID.ToString()).ThenBy(o => o.Name).Distinct().ToList();
        }

        private static async Task<List<CloudProvider>> DetectGenericCloudDrive()
        {
            var results = new List<CloudProvider>();
            using var clsidKey = Registry.ClassesRoot.OpenSubKey(@"CLSID");
            foreach (var subKeyName in clsidKey.GetSubKeyNames())
            {
                using var subKey = Extensions.IgnoreExceptions(() => clsidKey.OpenSubKey(subKeyName));
                if (subKey != null && (int?)subKey.GetValue("System.IsPinnedToNameSpaceTree") == 1)
                {
                    using var namespaceKey = Registry.CurrentUser.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{subKeyName}");
                    var driveType = (string)namespaceKey?.GetValue("");
                    if (driveType == null)
                    {
                        continue;
                    }

                    using var bagKey = subKey.OpenSubKey(@"Instance\InitPropertyBag");
                    var syncedFolder = (string)bagKey?.GetValue("TargetFolderPath");
                    if (syncedFolder == null)
                    {
                        continue;
                    }

                    // Also works for OneDrive, Box, Amazon Drive, iCloudDrive, Dropbox
                    CloudProviders? driveID = driveType switch
                    {
                        "MEGA" => CloudProviders.Mega,
                        "Amazon Drive" => CloudProviders.AmazonDrive,
                        _ => null
                    };
                    if (driveID == null)
                    {
                        continue;
                    }

                    results.Add(new CloudProvider()
                    {
                        ID = driveID.Value,
                        Name = driveID switch
                        {
                            CloudProviders.Mega => $"MEGA ({Path.GetFileName(syncedFolder.TrimEnd('\\'))})",
                            CloudProviders.AmazonDrive => $"Amazon Drive",
                            _ => null
                        },
                        SyncFolder = syncedFolder
                    });
                }
            }
            return await Task.FromResult(results);
        }

        private static async Task<List<CloudProvider>> DetectOneDrive()
        {
            using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts");

            if (oneDriveAccountsKey == null)
            {
                return null;
            }

            var oneDriveAccounts = new List<CloudProvider>();
            foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
            {
                var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
                var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
                var userFolder = (string)Registry.GetValue(accountKeyName, "UserFolder", null);
                var accountName = string.IsNullOrWhiteSpace(displayName) ? "OneDrive" : $"OneDrive - {displayName}";
                if (!string.IsNullOrWhiteSpace(userFolder) && !oneDriveAccounts.Any(x => x.Name == accountName))
                {
                    oneDriveAccounts.Add(new CloudProvider()
                    {
                        ID = CloudProviders.OneDrive,
                        Name = accountName,
                        SyncFolder = userFolder
                    });
                }
            }
            return await Task.FromResult(oneDriveAccounts);
        }

        private static async Task<List<CloudProvider>> DetectSharepoint()
        {
            using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts");

            if (oneDriveAccountsKey == null)
            {
                return null;
            }

            var sharepointAccounts = new List<CloudProvider>();

            foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
            {
                var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
                var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
                var userFolderToExcludeFromResults = (string)Registry.GetValue(accountKeyName, "UserFolder", null);
                var accountName = string.IsNullOrWhiteSpace(displayName) ? "SharePoint" : $"SharePoint - {displayName}";

                var sharePointSyncFolders = new List<string>();
                var mountPointKeyName = @$"SOFTWARE\Microsoft\OneDrive\Accounts\{account}\ScopeIdToMountPointPathCache";
                using (var mountPointsKey = Registry.CurrentUser.OpenSubKey(mountPointKeyName))
                {
                    if (mountPointsKey == null)
                    {
                        continue;
                    }

                    var valueNames = mountPointsKey.GetValueNames();
                    foreach (var valueName in valueNames)
                    {
                        var value = (string)Registry.GetValue(@$"HKEY_CURRENT_USER\{mountPointKeyName}", valueName, null);
                        if (!string.Equals(value, userFolderToExcludeFromResults, StringComparison.OrdinalIgnoreCase))
                        {
                            sharePointSyncFolders.Add(value);
                        }
                    }
                }

                foreach (var sharePointSyncFolder in sharePointSyncFolders.OrderBy(o => o))
                {
                    var parentFolder = Directory.GetParent(sharePointSyncFolder)?.FullName ?? string.Empty;
                    if (!sharepointAccounts.Any(acc => string.Equals(acc.Name, accountName, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(parentFolder))
                    {
                        sharepointAccounts.Add(new CloudProvider()
                        {
                            ID = CloudProviders.OneDriveCommercial,
                            Name = accountName,
                            SyncFolder = parentFolder
                        });
                    }
                }
            }

            return await Task.FromResult(sharepointAccounts);
        }
    }
}
