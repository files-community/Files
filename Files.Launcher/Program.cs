using Files.Common;
using Microsoft.Win32;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace FilesFullTrust
{
    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        private static void Main(string[] args)
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NLog.config"));
            LogManager.Configuration.Variables["LogPath"] = storageFolder.Path;

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            if (HandleCommandLineArgs())
            {
                // Handles OpenShellCommandInExplorer
                return;
            }

            // Only one instance of the fulltrust process allowed
            // This happens if multiple instances of the UWP app are launched
            using var mutex = new Mutex(true, "FilesUwpFullTrust", out bool isNew);
            if (!isNew)
            {
                return;
            }

            try
            {
                // Create handle table to store e.g. context menu references
                handleTable = new Win32API.DisposableDictionary();

                // Create shell COM object and get recycle bin folder
                recycler = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_RecycleBinFolder);
                ApplicationData.Current.LocalSettings.Values["RecycleBin_Title"] = recycler.Name;

                // Create filesystem watcher to monitor recycle bin folder(s)
                // SHChangeNotifyRegister only works if recycle bin is open in explorer :(
                binWatchers = new List<FileSystemWatcher>();
                var sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();
                foreach (var drive in DriveInfo.GetDrives())
                {
                    var recyclePath = Path.Combine(drive.Name, "$Recycle.Bin", sid);
                    if (drive.DriveType == DriveType.Network || !Directory.Exists(recyclePath))
                    {
                        continue;
                    }
                    var watcher = new FileSystemWatcher
                    {
                        Path = recyclePath,
                        Filter = "*.*",
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
                    };
                    watcher.Created += Watcher_Changed;
                    watcher.Deleted += Watcher_Changed;
                    watcher.EnableRaisingEvents = true;
                    binWatchers.Add(watcher);
                }

                // Preload context menu for better performace
                // We query the context menu for the app's local folder
                var preloadPath = ApplicationData.Current.LocalFolder.Path;
                using var _ = Win32API.ContextMenu.GetContextMenuForFiles(new string[] { preloadPath }, Shell32.CMF.CMF_NORMAL | Shell32.CMF.CMF_SYNCCASCADEMENU, FilterMenuItems(false));

                // Create cancellation token for drop window
                cancellation = new CancellationTokenSource();

                // Connect to app service and wait until the connection gets closed
                appServiceExit = new AutoResetEvent(false);
                InitializeAppServiceConnection();

                // Initialize device watcher
                deviceWatcher = new DeviceWatcher(connection);
                deviceWatcher.Start();

                // Wait until the connection gets closed
                appServiceExit.WaitOne();
            }
            finally
            {
                connection?.Dispose();
                foreach (var watcher in binWatchers)
                {
                    watcher.Dispose();
                }
                handleTable?.Dispose();
                recycler?.Dispose();
                deviceWatcher?.Dispose();
                cancellation?.Cancel();
                cancellation?.Dispose();
                appServiceExit?.Dispose();
                mutex?.ReleaseMutex();
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Logger.Error(exception, exception.Message);
        }

        private static async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"Recycle bin event: {e.ChangeType}, {e.FullPath}");
            if (e.Name.StartsWith("$I"))
            {
                // Recycle bin also stores a file starting with $I for each item
                return;
            }
            if (connection != null)
            {
                var response = new ValueSet()
                {
                    { "FileSystem", @"Shell:RecycleBinFolder" },
                    { "Path", e.FullPath },
                    { "Type", e.ChangeType.ToString() }
                };
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    using var folderItem = new ShellItem(e.FullPath);
                    var shellFileItem = GetRecycleBinItem(folderItem);
                    response["Item"] = JsonConvert.SerializeObject(shellFileItem);
                }
                // Send message to UWP app to refresh items
                await connection.SendMessageAsync(response);
            }
        }

        private static AppServiceConnection connection;
        private static AutoResetEvent appServiceExit;
        private static CancellationTokenSource cancellation;
        private static ShellFolder recycler;
        private static Win32API.DisposableDictionary handleTable;
        private static IList<FileSystemWatcher> binWatchers;
        private static DeviceWatcher deviceWatcher;

        private static async void InitializeAppServiceConnection()
        {
            connection = new AppServiceConnection();
            connection.AppServiceName = "FilesInteropService";
            connection.PackageFamilyName = Package.Current.Id.FamilyName;
            connection.RequestReceived += Connection_RequestReceived;
            connection.ServiceClosed += Connection_ServiceClosed;

            AppServiceConnectionStatus status = await connection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                // TODO: error handling
                connection?.Dispose();
                connection = null;
            }
        }

        private static async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral because we use an awaitable API below to respond to the message
            // and we don't want this call to get cancelled while we are waiting.
            var messageDeferral = args.GetDeferral();
            if (args.Request.Message == null)
            {
                messageDeferral.Complete();
                return;
            }

            try
            {
                if (args.Request.Message.ContainsKey("Arguments"))
                {
                    // This replaces launching the fulltrust process with arguments
                    // Instead a single instance of the process is running
                    // Requests from UWP app are sent via AppService connection
                    var arguments = (string)args.Request.Message["Arguments"];
                    var localSettings = ApplicationData.Current.LocalSettings;
                    Logger.Info($"Argument: {arguments}");

                    await ParseArgumentsAsync(args, messageDeferral, arguments, localSettings);
                }
                else if (args.Request.Message.ContainsKey("Application"))
                {
                    var application = (string)args.Request.Message["Application"];
                    HandleApplicationLaunch(application, args);
                }
                else if (args.Request.Message.ContainsKey("ApplicationList"))
                {
                    var applicationList = JsonConvert.DeserializeObject<IEnumerable<string>>((string)args.Request.Message["ApplicationList"]);
                    HandleApplicationsLaunch(applicationList, args);
                }
            }
            finally
            {
                // Complete the deferral so that the platform knows that we're done responding to the app service call.
                // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
                messageDeferral.Complete();
            }
        }

        private static async Task ParseArgumentsAsync(AppServiceRequestReceivedEventArgs args, AppServiceDeferral messageDeferral, string arguments, ApplicationDataContainer localSettings)
        {
            switch (arguments)
            {
                case "Terminate":
                    // Exit fulltrust process (UWP is closed or suspended)
                    appServiceExit.Set();
                    messageDeferral.Complete();
                    break;

                case "RecycleBin":
                    var binAction = (string)args.Request.Message["action"];
                    await ParseRecycleBinActionAsync(args, binAction);
                    break;

                case "DetectQuickLook":
                    // Check QuickLook Availability
                    var available = QuickLook.CheckQuickLookAvailability();
                    await args.Request.SendResponseAsync(new ValueSet()
                    {
                        { "IsAvailable", available }
                    });
                    break;

                case "ToggleQuickLook":
                    var path = (string)args.Request.Message["path"];
                    QuickLook.ToggleQuickLook(path);
                    break;

                case "ShellCommand":
                    // Kill the process. This is a BRUTAL WAY to kill a process.
#if DEBUG
                    // In debug mode this kills this process too??
#else
                    var pid = (int)args.Request.Message["pid"];
                    Process.GetProcessById(pid).Kill();
#endif

                    Process process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = "explorer.exe";
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.Arguments = (string)args.Request.Message["ShellCommand"];
                    process.Start();
                    break;

                case "LoadContextMenu":
                    var contextMenuResponse = new ValueSet();
                    var loadThreadWithMessageQueue = new Win32API.ThreadWithMessageQueue<ValueSet>(HandleMenuMessage);
                    var cMenuLoad = await loadThreadWithMessageQueue.PostMessageAsync<Win32API.ContextMenu>(args.Request.Message);
                    contextMenuResponse.Add("Handle", handleTable.AddValue(loadThreadWithMessageQueue));
                    contextMenuResponse.Add("ContextMenu", JsonConvert.SerializeObject(cMenuLoad));
                    await args.Request.SendResponseAsync(contextMenuResponse);
                    break;

                case "ExecAndCloseContextMenu":
                    var menuKey = (string)args.Request.Message["Handle"];
                    var execThreadWithMessageQueue = handleTable.GetValue<Win32API.ThreadWithMessageQueue<ValueSet>>(menuKey);
                    if (execThreadWithMessageQueue != null)
                    {
                        await execThreadWithMessageQueue.PostMessage(args.Request.Message);
                    }
                    // The following line is needed to cleanup resources when menu is closed.
                    // Unfortunately if you uncomment it some menu items will randomly stop working.
                    // Resource cleanup is currently done on app closing,
                    // if we find a solution for the issue above, we should cleanup as soon as a menu is closed.
                    //handleTable.RemoveValue(menuKey);
                    break;

                case "InvokeVerb":
                    var filePath = (string)args.Request.Message["FilePath"];
                    var split = filePath.Split('|').Where(x => !string.IsNullOrWhiteSpace(x));
                    using (var cMenu = Win32API.ContextMenu.GetContextMenuForFiles(split.ToArray(), Shell32.CMF.CMF_DEFAULTONLY))
                    {
                        cMenu?.InvokeVerb((string)args.Request.Message["Verb"]);
                    }
                    break;

                case "Bitlocker":
                    var bitlockerAction = (string)args.Request.Message["action"];
                    if (bitlockerAction == "Unlock")
                    {
                        var drive = (string)args.Request.Message["drive"];
                        var password = (string)args.Request.Message["password"];
                        Win32API.UnlockBitlockerDrive(drive, password);
                        await args.Request.SendResponseAsync(new ValueSet() { { "Bitlocker", "Unlock" } });
                    }
                    break;

                case "SetVolumeLabel":
                    var driveName = (string)args.Request.Message["drivename"];
                    var newLabel = (string)args.Request.Message["newlabel"];
                    Win32API.SetVolumeLabel(driveName, newLabel);
                    break;

                case "FileOperation":
                    await ParseFileOperationAsync(args);
                    break;

                case "GetIconOverlay":
                    var fileIconPath = (string)args.Request.Message["filePath"];
                    var thumbnailSize = (int)args.Request.Message["thumbnailSize"];
                    var iconOverlay = Win32API.GetFileIconAndOverlay(fileIconPath, thumbnailSize);
                    await args.Request.SendResponseAsync(new ValueSet()
                    {
                        { "Icon", iconOverlay.icon },
                        { "Overlay", iconOverlay.overlay },
                        { "HasCustomIcon", iconOverlay.isCustom }
                    });
                    break;

                case "GetOneDriveAccounts":
                    try
                    {
                        var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts", false);

                        if (oneDriveAccountsKey == null)
                        {
                            await args.Request.SendResponseAsync(new ValueSet());
                            return;
                        }

                        var oneDriveAccounts = new ValueSet();
                        foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
                        {
                            var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
                            var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
                            var userFolder = (string)Registry.GetValue(accountKeyName, "UserFolder", null);
                            var accountName = string.IsNullOrWhiteSpace(displayName) ? "OneDrive" : $"OneDrive - {displayName}";
                            if (!string.IsNullOrWhiteSpace(userFolder) && !oneDriveAccounts.ContainsKey(accountName))
                            {
                                oneDriveAccounts.Add(accountName, userFolder);
                            }
                        }
                        await args.Request.SendResponseAsync(oneDriveAccounts);
                    }
                    catch
                    {
                        await args.Request.SendResponseAsync(new ValueSet());
                    }
                    break;

                default:
                    if (args.Request.Message.ContainsKey("Application"))
                    {
                        var application = (string)args.Request.Message["Application"];
                        HandleApplicationLaunch(application, args);
                    }
                    else if (args.Request.Message.ContainsKey("ApplicationList"))
                    {
                        var applicationList = JsonConvert.DeserializeObject<IEnumerable<string>>((string)args.Request.Message["ApplicationList"]);
                        HandleApplicationsLaunch(applicationList, args);
                    }
                    break;
            }
        }

        private static object HandleMenuMessage(ValueSet message, Win32API.DisposableDictionary table)
        {
            switch ((string)message["Arguments"])
            {
                case "LoadContextMenu":
                    var contextMenuResponse = new ValueSet();
                    var filePath = (string)message["FilePath"];
                    var extendedMenu = (bool)message["ExtendedMenu"];
                    var showOpenMenu = (bool)message["ShowOpenMenu"];
                    var split = filePath.Split('|').Where(x => !string.IsNullOrWhiteSpace(x));
                    var cMenuLoad = Win32API.ContextMenu.GetContextMenuForFiles(split.ToArray(),
                        (extendedMenu ? Shell32.CMF.CMF_EXTENDEDVERBS : Shell32.CMF.CMF_NORMAL) | Shell32.CMF.CMF_SYNCCASCADEMENU, FilterMenuItems(showOpenMenu));
                    table.SetValue("MENU", cMenuLoad);
                    return cMenuLoad;

                case "ExecAndCloseContextMenu":
                    var cMenuExec = table.GetValue<Win32API.ContextMenu>("MENU");
                    if (message.TryGetValue("ItemID", out var menuId))
                    {
                        switch (message.Get("CommandString", (string)null))
                        {
                            case "format":
                                var drivePath = cMenuExec.ItemsPath.First();
                                Win32API.OpenFormatDriveDialog(drivePath);
                                break;

                            default:
                                cMenuExec?.InvokeItem((int)menuId);
                                break;
                        }
                    }
                    // The following line is needed to cleanup resources when menu is closed.
                    // Unfortunately if you uncomment it some menu items will randomly stop working.
                    // Resource cleanup is currently done on app closing,
                    // if we find a solution for the issue above, we should cleanup as soon as a menu is closed.
                    //table.RemoveValue("MENU");
                    return null;

                default:
                    return null;
            }
        }

        private static Func<string, bool> FilterMenuItems(bool showOpenMenu)
        {
            var knownItems = new List<string>()
            {
                "opennew", "openas", "opencontaining", "opennewprocess",
                "runas", "runasuser", "pintohome", "PinToStartScreen",
                "cut", "copy", "paste", "delete", "properties", "link",
                "Windows.ModernShare", "Windows.Share", "setdesktopwallpaper",
                "eject",
                Win32API.ExtractStringFromDLL("shell32.dll", 30312), // SendTo menu
                Win32API.ExtractStringFromDLL("shell32.dll", 34593), // Add to collection
            };

            bool filterMenuItemsImpl(string menuItem)
            {
                return string.IsNullOrEmpty(menuItem) ? false : knownItems.Contains(menuItem)
                    || (!showOpenMenu && menuItem.Equals("open", StringComparison.OrdinalIgnoreCase));
            }

            return filterMenuItemsImpl;
        }

        private static async Task ParseFileOperationAsync(AppServiceRequestReceivedEventArgs args)
        {
            if (args.Request.Message.ContainsKey("fileop"))
            {
                string fileOp = (string)args.Request.Message["fileop"];

                switch (fileOp)
                {
                    case "Clipboard":
                        await Win32API.StartSTATask(() =>
                        {
                            System.Windows.Forms.Clipboard.Clear();
                            var fileToCopy = (string)args.Request.Message["filepath"];
                            var operation = (DataPackageOperation)(int)args.Request.Message["operation"];
                            var fileList = new System.Collections.Specialized.StringCollection();
                            fileList.AddRange(fileToCopy.Split('|'));
                            if (operation == DataPackageOperation.Copy)
                            {
                                System.Windows.Forms.Clipboard.SetFileDropList(fileList);
                            }
                            else if (operation == DataPackageOperation.Move)
                            {
                                byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
                                MemoryStream dropEffect = new MemoryStream();
                                dropEffect.Write(moveEffect, 0, moveEffect.Length);
                                var data = new System.Windows.Forms.DataObject();
                                data.SetFileDropList(fileList);
                                data.SetData("Preferred DropEffect", dropEffect);
                                System.Windows.Forms.Clipboard.SetDataObject(data, true);
                            }
                            return true;
                        });
                        break;

                    case "DragDrop":
                        cancellation.Cancel();
                        cancellation.Dispose();
                        cancellation = new CancellationTokenSource();
                        var dropPath = (string)args.Request.Message["droppath"];
                        var dropText = (string)args.Request.Message["droptext"];
                        var drops = Win32API.StartSTATask<List<string>>(() =>
                        {
                            var form = new DragDropForm(dropPath, dropText, cancellation.Token);
                            System.Windows.Forms.Application.Run(form);
                            return form.DropTargets;
                        });
                        break;

                    case "DeleteItem":
                        var fileToDeletePath = (string)args.Request.Message["filepath"];
                        var permanently = (bool)args.Request.Message["permanently"];
                        using (var op = new ShellFileOperations())
                        {
                            op.Options = ShellFileOperations.OperationFlags.NoUI;
                            if (!permanently)
                            {
                                op.Options |= ShellFileOperations.OperationFlags.AllowUndo;
                            }
                            using var shi = new ShellItem(fileToDeletePath);
                            op.QueueDeleteOperation(shi);
                            var deleteTcs = new TaskCompletionSource<bool>();
                            op.PostDeleteItem += (s, e) => deleteTcs.TrySetResult(e.Result.Succeeded);
                            op.PerformOperations();
                            var result = await deleteTcs.Task;
                            await args.Request.SendResponseAsync(new ValueSet() {
                            { "Success", result } });
                        }
                        break;

                    case "ParseLink":
                        var linkPath = (string)args.Request.Message["filepath"];
                        try
                        {
                            if (linkPath.EndsWith(".lnk"))
                            {
                                using var link = new ShellLink(linkPath, LinkResolution.NoUIWithMsgPump, null, TimeSpan.FromMilliseconds(100));
                                await args.Request.SendResponseAsync(new ValueSet()
                                {
                                    { "TargetPath", link.TargetPath },
                                    { "Arguments", link.Arguments },
                                    { "WorkingDirectory", link.WorkingDirectory },
                                    { "RunAsAdmin", link.RunAsAdministrator },
                                    { "IsFolder", !string.IsNullOrEmpty(link.TargetPath) && link.Target.IsFolder }
                                });
                            }
                            else if (linkPath.EndsWith(".url"))
                            {
                                var linkUrl = await Win32API.StartSTATask(() =>
                                {
                                    var ipf = new Url.IUniformResourceLocator();
                                    (ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Load(linkPath, 0);
                                    ipf.GetUrl(out var retVal);
                                    return retVal;
                                });
                                await args.Request.SendResponseAsync(new ValueSet()
                                {
                                    { "TargetPath", linkUrl },
                                    { "Arguments", null },
                                    { "WorkingDirectory", null },
                                    { "RunAsAdmin", false },
                                    { "IsFolder", false }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Could not parse shortcut
                            Logger.Warn(ex, ex.Message);
                            await args.Request.SendResponseAsync(new ValueSet()
                            {
                                { "TargetPath", null },
                                { "Arguments", null },
                                { "WorkingDirectory", null },
                                { "RunAsAdmin", false },
                                { "IsFolder", false }
                            });
                        }
                        break;

                    case "CreateLink":
                    case "UpdateLink":
                        var linkSavePath = (string)args.Request.Message["filepath"];
                        var targetPath = (string)args.Request.Message["targetpath"];
                        if (linkSavePath.EndsWith(".lnk"))
                        {
                            var arguments = (string)args.Request.Message["arguments"];
                            var workingDirectory = (string)args.Request.Message["workingdir"];
                            var runAsAdmin = (bool)args.Request.Message["runasadmin"];
                            using var newLink = new ShellLink(targetPath, arguments, workingDirectory);
                            newLink.RunAsAdministrator = runAsAdmin;
                            newLink.SaveAs(linkSavePath); // Overwrite if exists
                        }
                        else if (linkSavePath.EndsWith(".url"))
                        {
                            await Win32API.StartSTATask(() =>
                            {
                                var ipf = new Url.IUniformResourceLocator();
                                ipf.SetUrl(targetPath, Url.IURL_SETURL_FLAGS.IURL_SETURL_FL_GUESS_PROTOCOL);
                                (ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Save(linkSavePath, false); // Overwrite if exists
                                return true;
                            });
                        }
                        break;
                }
            }
        }

        private static async Task ParseRecycleBinActionAsync(AppServiceRequestReceivedEventArgs args, string action)
        {
            switch (action)
            {
                case "Empty":
                    // Shell function to empty recyclebin
                    Shell32.SHEmptyRecycleBin(IntPtr.Zero, null, Shell32.SHERB.SHERB_NOCONFIRMATION | Shell32.SHERB.SHERB_NOPROGRESSUI);
                    break;

                case "Query":
                    var responseQuery = new ValueSet();
                    Win32API.SHQUERYRBINFO queryBinInfo = new Win32API.SHQUERYRBINFO();
                    queryBinInfo.cbSize = Marshal.SizeOf(queryBinInfo);
                    var res = Win32API.SHQueryRecycleBin("", ref queryBinInfo);
                    if (res == HRESULT.S_OK)
                    {
                        var numItems = queryBinInfo.i64NumItems;
                        var binSize = queryBinInfo.i64Size;
                        responseQuery.Add("NumItems", numItems);
                        responseQuery.Add("BinSize", binSize);
                        await args.Request.SendResponseAsync(responseQuery);
                    }
                    break;

                case "Enumerate":
                    // Enumerate recyclebin contents and send response to UWP
                    var responseEnum = new ValueSet();
                    var folderContentsList = new List<ShellFileItem>();
                    foreach (var folderItem in recycler)
                    {
                        try
                        {
                            // ShellLink Properties refer to destination and not shortcut
                            if (folderItem is ShellLink shellLink)
                            {
                                using var tempItem = new ShellItem(shellLink.PIDL);
                                var shellFileItem = GetRecycleBinItem(tempItem);
                                folderContentsList.Add(shellFileItem);
                            }
                            else
                            {
                                var shellFileItem = GetRecycleBinItem(folderItem);
                                folderContentsList.Add(shellFileItem);
                            }
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
                    responseEnum.Add("Enumerate", JsonConvert.SerializeObject(folderContentsList));
                    await args.Request.SendResponseAsync(responseEnum);
                    break;

                default:
                    break;
            }
        }

        private static ShellFileItem GetRecycleBinItem(ShellItem folderItem)
        {
            string recyclePath = folderItem.FileSystemPath; // True path on disk
            string fileName = Path.GetFileName(folderItem.Name); // Original file name
            string filePath = folderItem.Name; // Original file path + name
            bool isFolder = folderItem.IsFolder && Path.GetExtension(folderItem.Name) != ".zip";
            if (folderItem.Properties == null)
            {
                return new ShellFileItem(isFolder, recyclePath, fileName, filePath, DateTime.Now, DateTime.Now, DateTime.MinValue, null, 0, null);
            }
            folderItem.Properties.TryGetValue<System.Runtime.InteropServices.ComTypes.FILETIME?>(
                Ole32.PROPERTYKEY.System.Recycle.DateDeleted, out var fileTime);
            var recycleDate = fileTime?.ToDateTime().ToLocalTime() ?? DateTime.Now; // This is LocalTime
            folderItem.Properties.TryGetValue<System.Runtime.InteropServices.ComTypes.FILETIME?>(
                Ole32.PROPERTYKEY.System.DateModified, out fileTime);
            var modifiedDate = fileTime?.ToDateTime().ToLocalTime() ?? DateTime.Now; // This is LocalTime
            string fileSize = folderItem.Properties.TryGetValue<ulong?>(
                Ole32.PROPERTYKEY.System.Size, out var fileSizeBytes) ?
                folderItem.Properties.GetPropertyString(Ole32.PROPERTYKEY.System.Size) : null;
            folderItem.Properties.TryGetValue<string>(
                Ole32.PROPERTYKEY.System.ItemTypeText, out var fileType);
            return new ShellFileItem(isFolder, recyclePath, fileName, filePath, recycleDate, modifiedDate, DateTime.MinValue, fileSize, fileSizeBytes ?? 0, fileType);
        }

        private static void HandleApplicationsLaunch(IEnumerable<string> applications, AppServiceRequestReceivedEventArgs args)
        {
            foreach (var application in applications)
            {
                HandleApplicationLaunch(application, args);
            }
        }

        private static async void HandleApplicationLaunch(string application, AppServiceRequestReceivedEventArgs args)
        {
            var arguments = args.Request.Message.Get("Arguments", "");
            var workingDirectory = args.Request.Message.Get("WorkingDirectory", "");

            try
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = application;
                // Show window if workingDirectory (opening terminal)
                process.StartInfo.CreateNoWindow = string.IsNullOrEmpty(workingDirectory);
                if (arguments == "runas")
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas";
                    if (Path.GetExtension(application).ToLower() == ".msi")
                    {
                        process.StartInfo.FileName = "msiexec.exe";
                        process.StartInfo.Arguments = $"/a \"{application}\"";
                    }
                }
                else if (arguments == "runasuser")
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runasuser";
                    if (Path.GetExtension(application).ToLower() == ".msi")
                    {
                        process.StartInfo.FileName = "msiexec.exe";
                        process.StartInfo.Arguments = $"/i \"{application}\"";
                    }
                }
                else
                {
                    process.StartInfo.Arguments = arguments;
                }
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.Start();
            }
            catch (Win32Exception)
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas";
                process.StartInfo.FileName = application;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = workingDirectory;
                try
                {
                    process.Start();
                }
                catch (Win32Exception)
                {
                    try
                    {
                        await Win32API.StartSTATask(() =>
                        {
                            var split = application.Split('|').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => GetMtpPath(x));
                            if (split.Count() == 1)
                            {
                                Process.Start(split.First());
                            }
                            else
                            {
                                var groups = split.GroupBy(x => new
                                {
                                    Dir = Path.GetDirectoryName(x),
                                    Prog = Win32API.GetFileAssociationAsync(x).Result ?? Path.GetExtension(x)
                                });
                                foreach (var group in groups)
                                {
                                    if (!group.Any())
                                    {
                                        continue;
                                    }
                                    using var cMenu = Win32API.ContextMenu.GetContextMenuForFiles(group.ToArray(), Shell32.CMF.CMF_DEFAULTONLY);
                                    cMenu?.InvokeVerb(Shell32.CMDSTR_OPEN);
                                }
                            }
                            return true;
                        });
                    }
                    catch (Win32Exception)
                    {
                        // Cannot open file (e.g DLL)
                    }
                    catch (ArgumentException)
                    {
                        // Cannot open file (e.g DLL)
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Invalid file path
            }
        }

        private static bool HandleCommandLineArgs()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var arguments = (string)localSettings.Values["Arguments"];
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                localSettings.Values.Remove("Arguments");

                if (arguments == "ShellCommand")
                {
                    // Kill the process. This is a BRUTAL WAY to kill a process.
#if DEBUG
                    // In debug mode this kills this process too??
#else
                    var pid = (int)localSettings.Values["pid"];
                    Process.GetProcessById(pid).Kill();
#endif

                    Process process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = "explorer.exe";
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.Arguments = (string)localSettings.Values["ShellCommand"];
                    process.Start();

                    return true;
                }
            }
            return false;
        }

        private static string GetMtpPath(string executable)
        {
            if (executable.StartsWith("\\\\?\\"))
            {
                using var computer = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_ComputerFolder);
                using var device = computer.FirstOrDefault(i => executable.Replace("\\\\?\\", "").StartsWith(i.Name));
                var deviceId = device?.ParsingName;
                var itemPath = Regex.Replace(executable, @"^\\\\\?\\[^\\]*\\?", "");
                return deviceId != null ? Path.Combine(deviceId, itemPath) : executable;
            }
            return executable;
        }

        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            // Signal the event so the process can shut down
            appServiceExit.Set();
        }
    }
}