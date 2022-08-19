using Files.Shared;
using Files.Shared.Extensions;
using Files.FullTrust.Helpers;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class Win32MessageHandler : Disposable, IMessageHandler
    {
        private IList<FileSystemWatcher> dirWatchers;
        private PipeStream connection;
        private ShellFolder controlPanel, controlPanelCategoryView;

        public Win32MessageHandler()
        {
            dirWatchers = new List<FileSystemWatcher>();
            controlPanel = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_ControlPanelFolder);
            controlPanelCategoryView = new ShellFolder("::{26EE0668-A00A-44D7-9371-BEB064C98683}");
        }

        public void Initialize(PipeStream connection)
        {
            this.connection = connection;

            DetectIsSetAsDefaultFileManager();
            DetectIsSetAsOpenFileDialog();
            ApplicationData.Current.LocalSettings.Values["TEMP"] = Environment.GetEnvironmentVariable("TEMP");
        }

        private static void DetectIsSetAsDefaultFileManager()
        {
            using var subkey = Registry.ClassesRoot.OpenSubKey(@"Folder\shell\open\command");
            var command = (string)subkey?.GetValue(string.Empty);
            ApplicationData.Current.LocalSettings.Values["IsSetAsDefaultFileManager"] = !string.IsNullOrEmpty(command) && command.Contains("FilesLauncher.exe");
        }

        private static void DetectIsSetAsOpenFileDialog()
        {
            using var subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}");
            ApplicationData.Current.LocalSettings.Values["IsSetAsOpenFileDialog"] = subkey?.GetValue(string.Empty) as string == "FilesOpenDialog class";
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "Bitlocker":
                    var bitlockerAction = (string)message["action"];
                    if (bitlockerAction == "Unlock")
                    {
                        var drive = (string)message["drive"];
                        var password = (string)message["password"];
                        Win32API.UnlockBitlockerDrive(drive, password);
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Bitlocker", "Unlock" } }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "SetVolumeLabel":
                    var driveName = (string)message["drivename"];
                    var newLabel = (string)message["newlabel"];
                    Win32API.SetVolumeLabel(driveName, newLabel);
                    await Win32API.SendMessageAsync(connection, new ValueSet() { { "SetVolumeLabel", driveName } }, message.Get("RequestID", (string)null));
                    break;

                case "GetIconOverlay":
                    var fileIconPath = (string)message["filePath"];
                    var thumbnailSize = (int)(long)message["thumbnailSize"];
                    var isOverlayOnly = (bool)message["isOverlayOnly"];
                    var (icon, overlay) = await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(fileIconPath, thumbnailSize, true, isOverlayOnly));
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "Icon", icon },
                        { "Overlay", overlay }
                    }, message.Get("RequestID", (string)null));
                    break;

                case "GetIconWithoutOverlay":
                    var fileIconPath2 = (string)message["filePath"];
                    var thumbnailSize2 = (int)(long)message["thumbnailSize"];
                    var icon2 = await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(fileIconPath2, thumbnailSize2, false));
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "Icon", icon2.icon },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "ShellItem":
                    var itemPath = (string)message["item"];
                    var siAction = (string)message["action"];
                    var siResponseEnum = new ValueSet();
                    var item = await Win32API.StartSTATask(() =>
                    {
                        using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPidl(itemPath);
                        return ShellFolderExtensions.GetShellFileItem(shellItem);
                    });
                    siResponseEnum.Add("Item", JsonConvert.SerializeObject(item));
                    await Win32API.SendMessageAsync(connection, siResponseEnum, message.Get("RequestID", (string)null));
                    break;

                case "ShellFolder":
                    var folderPath = (string)message["folder"];
                    if (folderPath.StartsWith("::{", StringComparison.Ordinal))
                    {
                        folderPath = $"shell:{folderPath}";
                    }
                    var sfAction = (string)message["action"];
                    var fromIndex = (int)message.Get("from", 0L);
                    var maxItems = (int)message.Get("count", (long)int.MaxValue);
                    var sfResponseEnum = new ValueSet();
                    var (folder, folderContentsList) = await Win32API.StartSTATask(() =>
                    {
                        var flc = new List<ShellFileItem>();
                        var folder = (ShellFileItem)null;
                        try
                        {
                            using var shellFolder = ShellFolderExtensions.GetShellItemFromPathOrPidl(folderPath) as ShellFolder;
                            folder = ShellFolderExtensions.GetShellFileItem(shellFolder);
                            if ((controlPanel.PIDL.IsParentOf(shellFolder.PIDL, false) || controlPanelCategoryView.PIDL.IsParentOf(shellFolder.PIDL, false)) 
                                && !shellFolder.Any())
                            {
                                // Return null to force open unsupported items in explorer
                                // Only if inside control panel and folder appears empty
                                return (null, flc);
                            }
                            if (sfAction == "Enumerate")
                            {
                                foreach (var folderItem in shellFolder.Skip(fromIndex).Take(maxItems))
                                {
                                    try
                                    {
                                        var shellFileItem = folderItem is ShellLink link ?
                                            ShellFolderExtensions.GetShellLinkItem(link) :
                                            ShellFolderExtensions.GetShellFileItem(folderItem);
                                        flc.Add(shellFileItem);
                                    }
                                    catch (FileNotFoundException)
                                    {
                                        // Happens if files are being deleted
                                    }
                                    finally
                                    {
                                        folderItem.Dispose();
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                        return (folder, flc);
                    });
                    sfResponseEnum.Add("Folder", JsonConvert.SerializeObject(folder));
                    sfResponseEnum.Add("Enumerate", JsonConvert.SerializeObject(folderContentsList, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects
                    }));
                    await Win32API.SendMessageAsync(connection, sfResponseEnum, message.Get("RequestID", (string)null));
                    break;

                case "GetFolderIconsFromDLL":
                    var iconInfos = Win32API.ExtractIconsFromDLL((string)message["iconFile"]);
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "IconInfos", JsonConvert.SerializeObject(iconInfos) },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "SetCustomFolderIcon":
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "Success", Win32API.SetCustomDirectoryIcon((string)message["folder"], (string)message["iconFile"], (int)message.Get("iconIndex", 0L)) },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "GetSelectedIconsFromDLL":
                    var selectedIconInfos = Win32API.ExtractSelectedIconsFromDLL((string)message["iconFile"], JsonConvert.DeserializeObject<List<int>>((string)message["iconIndexes"]), Convert.ToInt32(message["requestedIconSize"]));
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "IconInfos", JsonConvert.SerializeObject(selectedIconInfos) },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "SetAsDefaultExplorer":
                    {
                        var enable = (bool)message["Value"];
                        var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
                        Directory.CreateDirectory(destFolder);
                        foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledLocation.Path, "Files.FullTrust", "Assets", "FilesOpenDialog")))
                        {
                            if (!SafetyExtensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), Program.Logger))
                            {
                                // Error copying files
                                DetectIsSetAsDefaultFileManager();
                                await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                                return;
                            }
                        }

                        var dataPath = Environment.ExpandEnvironmentVariables("%LocalAppData%\\Files");
                        if (enable)
                        {
                            if (!Win32API.RunPowershellCommand($"-command \"New-Item -Force -Path '{dataPath}' -ItemType Directory; Copy-Item -Filter *.* -Path '{destFolder}\\*' -Recurse -Force -Destination '{dataPath}'\"", false))
                            {
                                // Error copying files
                                DetectIsSetAsDefaultFileManager();
                                await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                                return;
                            }
                        }
                        else
                        {
                            Win32API.RunPowershellCommand($"-command \"Remove-Item -Path '{dataPath}' -Recurse -Force\"", false);
                        }

                        try
                        {
                            using var regProcess = Process.Start(new ProcessStartInfo("regedit.exe", @$"/s ""{Path.Combine(destFolder, enable ? "SetFilesAsDefault.reg" : "UnsetFilesAsDefault.reg")}""") { UseShellExecute = true, Verb = "runas" });
                            regProcess.WaitForExit();
                            DetectIsSetAsDefaultFileManager();
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", true } }, message.Get("RequestID", (string)null));
                        }
                        catch
                        {
                            // Canceled UAC
                            DetectIsSetAsDefaultFileManager();
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                        }
                    }
                    break;

                case "SetAsOpenFileDialog":
                    {
                        var enable = (bool)message["Value"];
                        var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
                        Directory.CreateDirectory(destFolder);
                        foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledLocation.Path, "Files.FullTrust", "Assets", "FilesOpenDialog")))
                        {
                            if (!SafetyExtensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), Program.Logger))
                            {
                                // Error copying files
                                DetectIsSetAsOpenFileDialog();
                                await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                                return;
                            }
                        }

                        try
                        {
                            using var regProc32 = Process.Start("regsvr32.exe", @$"/s /n {(!enable ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomOpenDialog32.dll")}""");
                            regProc32.WaitForExit();
                            using var regProc64 = Process.Start("regsvr32.exe", @$"/s /n {(!enable ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomOpenDialog64.dll")}""");
                            regProc64.WaitForExit();
                            using var regProcARM64 = Process.Start("regsvr32.exe", @$"/s /n {(!enable ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomOpenDialogARM64.dll")}""");
                            regProcARM64.WaitForExit();

                            DetectIsSetAsOpenFileDialog();
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", true } }, message.Get("RequestID", (string)null));
                        }
                        catch
                        {
                            DetectIsSetAsOpenFileDialog();
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                        }
                    }
                    break;

                case "GetFileAssociation":
                    {
                        var filePath = (string)message["filepath"];
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "FileAssociation", await Win32API.GetFileAssociationAsync(filePath, true) } }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "WatchDirectory":
                    var watchAction = (string)message["action"];
                    await ParseWatchDirectoryActionAsync(connection, message, watchAction);
                    break;
            }
        }

        private async Task ParseWatchDirectoryActionAsync(PipeStream connection, Dictionary<string, object> message, string action)
        {
            switch (action)
            {
                case "start":
                    {
                        var res = new ValueSet();
                        var folderPath = (string)message["folderPath"];
                        if (Directory.Exists(folderPath))
                        {
                            var watcher = new FileSystemWatcher
                            {
                                Path = folderPath,
                                Filter = "*.*",
                                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
                            };
                            watcher.Created += DirectoryWatcher_Changed;
                            watcher.Deleted += DirectoryWatcher_Changed;
                            watcher.Renamed += DirectoryWatcher_Changed;
                            watcher.EnableRaisingEvents = true;
                            res.Add("watcherID", watcher.GetHashCode());
                            dirWatchers.Add(watcher);
                        }
                        await Win32API.SendMessageAsync(connection, res, message.Get("RequestID", (string)null));
                    }
                    break;

                case "cancel":
                    {
                        var watcherID = (long)message["watcherID"];
                        var watcher = dirWatchers.SingleOrDefault(x => x.GetHashCode() == watcherID);
                        if (watcher != null)
                        {
                            dirWatchers.Remove(watcher);
                            watcher.Dispose();
                        }
                    }
                    break;
            }
        }

        private async void DirectoryWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Directory watcher event: {e.ChangeType}, {e.FullPath}");
            if (connection?.IsConnected ?? false)
            {
                var response = new ValueSet()
                {
                    { "FileSystem", Path.GetDirectoryName(e.FullPath) },
                    { "Name", e.Name },
                    { "Path", e.FullPath },
                    { "Type", e.ChangeType.ToString() },
                    { "WatcherID", sender.GetHashCode() },
                };
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    var shellFileItem = await GetShellFileItemAsync(e.FullPath);
                    if (shellFileItem == null) return;
                    response["Item"] = JsonConvert.SerializeObject(shellFileItem);
                }
                else if (e.ChangeType == WatcherChangeTypes.Renamed)
                {
                    response["OldPath"] = (e as RenamedEventArgs).OldFullPath;
                }
                // Send message to UWP app to refresh items
                await Win32API.SendMessageAsync(connection, response);
            }
        }

        private async Task<ShellFileItem> GetShellFileItemAsync(string fullPath)
        {
            while (true)
            {
                using var hFile = Kernel32.CreateFile(fullPath, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
                if (!hFile.IsInvalid)
                {
                    using var folderItem = SafetyExtensions.IgnoreExceptions(() => new ShellItem(fullPath));
                    if (folderItem == null) return null;
                    return ShellFolderExtensions.GetShellFileItem(folderItem);
                }
                var lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (lastError != Win32Error.ERROR_SHARING_VIOLATION && lastError != Win32Error.ERROR_LOCK_VIOLATION)
                {
                    return null;
                }
                await Task.Delay(200);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var watcher in dirWatchers)
                {
                    watcher.Dispose();
                }
                controlPanel.Dispose();
                controlPanelCategoryView.Dispose();
            }
        }
    }
}
