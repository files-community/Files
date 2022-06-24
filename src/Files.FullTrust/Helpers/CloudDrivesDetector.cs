using Files.Shared.Cloud;
using Files.Shared.Extensions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Files.FullTrust.Helpers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class CloudDrivesDetector
    {
        public static async Task<IEnumerable<ICloudProvider>> DetectCloudDrives()
        {
            var tasks = new Task<IEnumerable<ICloudProvider>>[]
            {
                SafetyExtensions.IgnoreExceptions(DetectOneDrive, Program.Logger),
                SafetyExtensions.IgnoreExceptions(DetectSharepoint, Program.Logger),
                SafetyExtensions.IgnoreExceptions(DetectGenericCloudDrive, Program.Logger),
                SafetyExtensions.IgnoreExceptions(DetectYandexDisk, Program.Logger),
            };

            await Task.WhenAll(tasks);

            return tasks
                .Where(o => o.Result is not null)
                .SelectMany(o => o.Result)
                .OrderBy(o => o.ID.ToString())
                .ThenBy(o => o.Name)
                .Distinct();
        }

        private static Task<IEnumerable<ICloudProvider>> DetectYandexDisk()
        {
            var results = new List<ICloudProvider>();
            using var yandexKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Yandex\Yandex.Disk.2");

            var syncedFolder = (string)yandexKey?.GetValue("RootFolder");
            if (syncedFolder is not null)
            {
                results.Add(new CloudProvider(CloudProviders.Yandex)
                {
                    Name = $"Yandex Disk",
                    SyncFolder = syncedFolder,
                });
            }

            return Task.FromResult<IEnumerable<ICloudProvider>>(results);
        }

        private static Task<IEnumerable<ICloudProvider>> DetectGenericCloudDrive()
        {
            var results = new List<ICloudProvider>();
            using var clsidKey = Registry.ClassesRoot.OpenSubKey(@"CLSID");
            using var namespaceKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace");

            foreach (var subKeyName in namespaceKey.GetSubKeyNames())
            {
                using var clsidSubKey = SafetyExtensions.IgnoreExceptions(() => clsidKey.OpenSubKey(subKeyName));
                if (clsidSubKey is not null && (int?)clsidSubKey.GetValue("System.IsPinnedToNameSpaceTree") is 1)
                {
                    using var namespaceSubKey = namespaceKey.OpenSubKey(subKeyName);
                    var driveType = (string)namespaceSubKey?.GetValue(string.Empty);
                    if (driveType is null)
                    {
                        continue;
                    }

                    //Nextcloud specific
                    var appName = (string)namespaceSubKey?.GetValue("ApplicationName");
                    if (!string.IsNullOrEmpty(appName) && appName == "Nextcloud")
                    {
                        driveType = appName;
                    }

                    using var bagKey = clsidSubKey.OpenSubKey(@"Instance\InitPropertyBag");
                    var syncedFolder = (string)bagKey?.GetValue("TargetFolderPath");
                    if (syncedFolder is null)
                    {
                        continue;
                    }

                    // Also works for OneDrive, Box, iCloudDrive, Dropbox
                    CloudProviders? driveID = driveType switch
                    {
                        "MEGA" => CloudProviders.Mega,
                        "Amazon Drive" => CloudProviders.AmazonDrive,
                        "Nextcloud" => CloudProviders.Nextcloud,
                        "Jottacloud" => CloudProviders.Jottacloud,
                        _ => null,
                    };
                    if (driveID is null)
                    {
                        continue;
                    }

                    string nextCloudValue = (string)namespaceSubKey?.GetValue(string.Empty);
                    results.Add(new CloudProvider(driveID.Value)
                    {
                        Name = driveID switch
                        {
                            CloudProviders.Mega => $"MEGA ({Path.GetFileName(syncedFolder.TrimEnd('\\'))})",
                            CloudProviders.AmazonDrive => $"Amazon Drive",
                            CloudProviders.Nextcloud => !string.IsNullOrEmpty(nextCloudValue) ? nextCloudValue : "Nextcloud",
                            CloudProviders.Jottacloud => $"Jottacloud",
                            _ => null
                        },
                        SyncFolder = syncedFolder,
                    });
                }
            }

            return Task.FromResult<IEnumerable<ICloudProvider>>(results);
        }

        private static Task<IEnumerable<ICloudProvider>> DetectOneDrive()
        {
            using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts");
            if (oneDriveAccountsKey is null)
            {
                return Task.FromResult<IEnumerable<ICloudProvider>>(null);
            }

            var oneDriveAccounts = new List<ICloudProvider>();
            foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
            {
                var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
                var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
                var userFolder = (string)Registry.GetValue(accountKeyName, "UserFolder", null);
                var accountName = string.IsNullOrWhiteSpace(displayName) ? "OneDrive" : $"OneDrive - {displayName}";

                if (!string.IsNullOrWhiteSpace(userFolder) && !oneDriveAccounts.Any(x => x.Name == accountName))
                {
                    oneDriveAccounts.Add(new CloudProvider(CloudProviders.OneDrive)
                    {
                        Name = accountName,
                        SyncFolder = userFolder,
                    });
                }
            }

            return Task.FromResult<IEnumerable<ICloudProvider>>(oneDriveAccounts);
        }

        private static Task<IEnumerable<ICloudProvider>> DetectSharepoint()
        {
            using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts");
            if (oneDriveAccountsKey is null)
            {
                return Task.FromResult<IEnumerable<ICloudProvider>>(null);
            }

            var sharepointAccounts = new List<ICloudProvider>();
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
                    if (mountPointsKey is null)
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

                sharePointSyncFolders.Sort(StringComparer.Ordinal);
                foreach (var sharePointSyncFolder in sharePointSyncFolders)
                {
                    var parentFolder = Directory.GetParent(sharePointSyncFolder)?.FullName ?? string.Empty;
                    if (!sharepointAccounts.Any(acc =>
                        string.Equals(acc.Name, accountName, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(parentFolder))
                    {
                        sharepointAccounts.Add(new CloudProvider(CloudProviders.OneDriveCommercial)
                        {
                            Name = accountName,
                            SyncFolder = parentFolder,
                        });
                    }
                }
            }

            return Task.FromResult<IEnumerable<ICloudProvider>>(sharepointAccounts);
        }
    }
}
