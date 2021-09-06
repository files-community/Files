using Files.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud.Providers
{
    public class GenericCloudProvider : ICloudProviderDetector
    {
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            try
            {
                return await Task.Run(() => DetectFromRegistry());
            }
            catch
            {
                // Not detected
                return Array.Empty<CloudProvider>();
            }
        }

        private IList<CloudProvider> DetectFromRegistry()
        {
            var results = new List<CloudProvider>();
            using var clsidKey = Registry.ClassesRoot.OpenSubKey(@"CLSID");
            foreach (var subKeyName in clsidKey.GetSubKeyNames())
            {
                using var subKey = clsidKey.OpenSubKey(subKeyName);
                if ((int?)subKey.GetValue("System.IsPinnedToNameSpaceTree") == 1)
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
                        var knownFolder = (string)bagKey?.GetValue("TargetKnownFolder");
                        if (knownFolder != null)
                        {
                            using var folderKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
                            syncedFolder = (string)folderKey?.GetValue($"{knownFolder}");
                        }
                    }
                    if (syncedFolder == null)
                    {
                        continue;
                    }

                    CloudProviders? driveID = driveType switch
                    {
                        "MEGA" => CloudProviders.Mega,
                        "Amazon Drive" => CloudProviders.AmazonDrive,
                        "Dropbox" => CloudProviders.DropBox,
                        string s when s.StartsWith("iCloudDrive") => CloudProviders.AppleCloud,
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
                            CloudProviders.AmazonDrive => "Amazon Drive",
                            CloudProviders.AppleCloud => "iCloud",
                            CloudProviders.DropBox => "Dropbox",
                            CloudProviders.Mega => $"MEGA ({System.IO.Path.GetFileName(syncedFolder.TrimEnd('\\'))})",
                            _ => null
                        },
                        SyncFolder = syncedFolder
                    });
                }
            }
            return results;
        }
    }
}
