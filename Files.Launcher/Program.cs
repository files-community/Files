#nullable enable
using Files.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vanara.Windows.Shell;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using static Vanara.PInvoke.Shell32;

namespace FilesFullTrust
{
    internal class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [STAThread]
        private static void Main(string[] args)
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NLog.config"));
            NLog.LogManager.Configuration.Variables["LogPath"] = storageFolder.Path;

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            if (HandleCommandLineArgs())
            {
                // Handles OpenShellCommandInExplorer
                return;
            }

            // Only one instance of the fulltrust process allowed
            // This happens if multiple instances of the UWP app are launched
            using var mutex = new Mutex(true, "FilesUwpFullTrust", out bool isNew);
            if (!isNew) return;

            try
            {
                // Create shell COM object and get recycle bin folder
                recycler = new ShellFolder(KNOWNFOLDERID.FOLDERID_RecycleBinFolder);
                ApplicationData.Current.LocalSettings.Values["RecycleBin_Title"] = recycler.Name;

                // Create filesystem watcher to monitor recycle bin folder(s)
                // SHChangeNotifyRegister only works if recycle bin is open in explorer :(
                watchers = new List<FileSystemWatcher>();
                var sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();
                foreach (var drive in DriveInfo.GetDrives())
                {
                    var recycle_path = Path.Combine(drive.Name, "$Recycle.Bin", sid);
                    if (!Directory.Exists(recycle_path))
                    {
                        continue;
                    }
                    var watcher = new FileSystemWatcher();
                    watcher.Path = recycle_path;
                    watcher.Filter = "*.*";
                    watcher.NotifyFilter = NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName;
                    watcher.Created += Watcher_Changed;
                    watcher.Deleted += Watcher_Changed;
                    watcher.EnableRaisingEvents = true;
                    watchers.Add(watcher);
                }

                // Connect to app service and wait until the connection gets closed
                appServiceExit = new AutoResetEvent(false);
                InitializeAppServiceConnection();
                appServiceExit.WaitOne();
            }
            finally
            {
                connection?.Dispose();
                if (watchers != null)
                {
                    foreach (var watcher in watchers)
                    {
                        watcher.Dispose();
                    }
                }
                recycler?.Dispose();
                appServiceExit?.Dispose();
                mutex?.ReleaseMutex();
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                Logger.Error(exception, exception.Message);
            }
        }

        private static async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine("Reycle bin event: {0}, {1}", e.ChangeType, e.FullPath);
            if (connection != null)
            {
                // Send message to UWP app to refresh items
                await connection.SendMessageAsync(new ValueSet() {
                    { "FileSystem", @"Shell:RecycleBinFolder" },
                    { "Path", e.FullPath },
                    { "Type", e.ChangeType.ToString() }
                });
            }
        }

        private static AppServiceConnection? connection;
        private static AutoResetEvent? appServiceExit;
        private static ShellFolder? recycler;
        private static IList<FileSystemWatcher>? watchers;

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
                connection.Dispose();
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

                    await parseArguments(args, messageDeferral, arguments, localSettings);
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

        private static async Task parseArguments(AppServiceRequestReceivedEventArgs args, AppServiceDeferral messageDeferral, string arguments, ApplicationDataContainer localSettings)
        {
            switch (arguments)
            {
                case "Terminate":
                    // Exit fulltrust process (UWP is closed or suspended)
                    appServiceExit?.Set();
                    messageDeferral.Complete();
                    break;

                case "RecycleBin":
                    var binAction = (string)args.Request.Message["action"];
                    await parseRecycleBinAction(args, binAction);
                    break;

                case "StartupTasks":
                    // Check QuickLook Availability
                    QuickLook.CheckQuickLookAvailability(localSettings);
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

                case "LoadMUIVerb":
                    var responseSet = new ValueSet();
                    responseSet.Add("MUIVerbString", Win32API.ExtractStringFromDLL((string)args.Request.Message["MUIVerbLocation"], (int)args.Request.Message["MUIVerbLine"]));
                    await args.Request.SendResponseAsync(responseSet);
                    break;

                case "ParseAguments":
                    var responseArray = new ValueSet();
                    var resultArgument = Win32API.CommandLineToArgs((string)args.Request.Message["Command"]);
                    responseArray.Add("ParsedArguments", JsonConvert.SerializeObject(resultArgument));
                    await args.Request.SendResponseAsync(responseArray);
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

        private static async Task parseRecycleBinAction(AppServiceRequestReceivedEventArgs args, string action)
        {
            switch (action)
            {
                case "Empty":
                    // Shell function to empty recyclebin
                    SHEmptyRecycleBin(IntPtr.Zero, null, SHERB.SHERB_NOCONFIRMATION | SHERB.SHERB_NOPROGRESSUI);
                    break;

                case "Query":
                    var responseQuery = new ValueSet();
                    SHQUERYRBINFO queryBinInfo = new SHQUERYRBINFO();
                    queryBinInfo.cbSize = (uint)Marshal.SizeOf(queryBinInfo);
                    var res = SHQueryRecycleBin(null, ref queryBinInfo);
                    if (res == Vanara.PInvoke.HRESULT.S_OK)
                    {
                        var numItems = queryBinInfo.i64NumItems;
                        var binSize = queryBinInfo.i64Size;
                        responseQuery.Add("NumItems", numItems);
                        responseQuery.Add("BinSize", binSize);
                        responseQuery.Add("FileOwner", (string?)recycler?.Properties[Vanara.PInvoke.Ole32.PROPERTYKEY.System.FileOwner]);
                        if (watchers.Any())
                        {
                            var info = new DirectoryInfo(watchers.First().Path);
                            responseQuery.Add("DateAccessed", info.LastAccessTime.ToBinary());
                            responseQuery.Add("DateCreated", info.CreationTime.ToBinary());
                        }
                        await args.Request.SendResponseAsync(responseQuery);
                    }
                    break;

                case "Enumerate":
                    // Enumerate recyclebin contents and send response to UWP
                    var responseEnum = new ValueSet();
                    var folderContentsList = new List<ShellFileItem>();
                    if (recycler != null)
                    {
                        foreach (var folderItem in recycler)
                        {
                            try
                            {
                                string recyclePath = folderItem.FileSystemPath; // True path on disk
                                string fileName = Path.GetFileName(folderItem.Name); // Original file name
                                string filePath = folderItem.Name; // Original file path + name
                                bool isFolder = folderItem.IsFolder && Path.GetExtension(folderItem.Name) != ".zip";
                                if (folderItem.Properties == null)
                                {
                                    folderContentsList.Add(new ShellFileItem(isFolder, recyclePath, fileName, filePath, DateTime.Now, null, 0, null));
                                    continue;
                                }
                                folderItem.Properties.TryGetValue<System.Runtime.InteropServices.ComTypes.FILETIME?>(
                                    Vanara.PInvoke.Ole32.PROPERTYKEY.System.DateCreated, out var fileTime);
                                var recycleDate = fileTime?.ToDateTime().ToLocalTime() ?? DateTime.Now; // This is LocalTime
                                string? fileSize = folderItem.Properties.TryGetValue<ulong?>(
                                    Vanara.PInvoke.Ole32.PROPERTYKEY.System.Size, out var fileSizeBytes) ?
                                    folderItem.Properties.GetPropertyString(Vanara.PInvoke.Ole32.PROPERTYKEY.System.Size) : null;
                                folderItem.Properties.TryGetValue<string>(
                                    Vanara.PInvoke.Ole32.PROPERTYKEY.System.ItemTypeText, out var fileType);
                                folderContentsList.Add(new ShellFileItem(isFolder, recyclePath, fileName, filePath, recycleDate, fileSize, fileSizeBytes ?? 0, fileType));
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
                    responseEnum.Add("Enumerate", JsonConvert.SerializeObject(folderContentsList));
                    await args.Request.SendResponseAsync(responseEnum);
                    break;

                default:
                    break;
            }
        }

        private static void HandleApplicationsLaunch(IEnumerable<string> applications, AppServiceRequestReceivedEventArgs args)
        {
            foreach (var application in applications)
            {
                HandleApplicationLaunch(application, args);
            }
        }

        private static void HandleApplicationLaunch(string application, AppServiceRequestReceivedEventArgs args)
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
                        var split = application.Split(';').Where(x => !string.IsNullOrWhiteSpace(x));
                        if (split.Count() == 1)
                        {
                            Process.Start(application);
                        }
                        else
                        {
                            var groups = split.GroupBy(x => new
                            {
                                Dir = Path.GetDirectoryName(x),
                                Prog = Win32API.GetFileAssociation(x).Result ?? Path.GetExtension(x)
                            });
                            foreach (var group in groups)
                            {
                                if (!group.Any()) continue;
                                var files = group.Select(x => new ShellItem(x));
                                using var sf = files.First().Parent;
                                IContextMenu? menu = null;
                                try
                                {
                                    menu = sf.GetChildrenUIObjects<IContextMenu>(null, files.ToArray());
                                    menu.QueryContextMenu(Vanara.PInvoke.HMENU.NULL, 0, 0, 0, CMF.CMF_DEFAULTONLY);
                                    var pici = new CMINVOKECOMMANDINFOEX();
                                    pici.lpVerb = CMDSTR_OPEN;
                                    pici.nShow = Vanara.PInvoke.ShowWindowCommand.SW_SHOW;
                                    pici.cbSize = (uint)Marshal.SizeOf(pici);
                                    menu.InvokeCommand(pici);
                                }
                                finally
                                {
                                    foreach (var elem in files)
                                        elem.Dispose();
                                    if (menu != null)
                                        Marshal.ReleaseComObject(menu);
                                }
                            }
                        }
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
                else if (arguments == "StartupTasks")
                {
                    // Check QuickLook Availability
                    QuickLook.CheckQuickLookAvailability(localSettings);
                    return true;
                }
            }
            return false;
        }

        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            // Signal the event so the process can shut down
            appServiceExit?.Set();
        }
    }
}
