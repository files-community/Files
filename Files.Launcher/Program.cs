using Files.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Vanara.Windows.Shell;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace FilesFullTrust
{
    internal class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [STAThread]
        private static void Main(string[] args)
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
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
            var mutex = new Mutex(true, "FilesUwpFullTrust", out bool isNew);
            if (!isNew) return;

            // Create shell COM object and get recycle bin folder
            recycler = new ShellFolder(Vanara.PInvoke.Shell32.KNOWNFOLDERID.FOLDERID_RecycleBinFolder);
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["RecycleBin_Title"] = recycler.Name;

            // Create shell watcher to monitor recycle bin folder
            var watcher = new ShellItemChangeWatcher(recycler, false);
            watcher.NotifyFilter = ChangeFilters.AllDiskEvents;
            watcher.Changed += Watcher_Changed;
            //watcher.EnableRaisingEvents = true; // TODO: uncomment this when updated library is released

            try
            {
                // Connect to app service and wait until the connection gets closed
                appServiceExit = new AutoResetEvent(false);
                InitializeAppServiceConnection();
                appServiceExit.WaitOne();
            }
            finally
            {
                connection?.Dispose();
                watcher?.Dispose();
                recycler?.Dispose();
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Logger.Error(exception, exception.Message);
        }

        private static bool HandleCommandLineArgs()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
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

        private static async void Watcher_Changed(object sender, ShellItemChangeWatcher.ShellItemChangeEventArgs e)
        {
            Console.WriteLine($"File: {e.ChangedItems.FirstOrDefault()?.FileSystemPath} {e.ChangeType}");
            if (connection != null)
            {
                // Send message to UWP app to refresh items
                await connection.SendMessageAsync(new ValueSet() { { "FileSystem", @"Shell:RecycleBinFolder" }, { "Path", e.ChangedItems.FirstOrDefault()?.FileSystemPath }, { "Type", e.ChangeType.ToString() } });
            }
        }

        private static AppServiceConnection connection;
        private static AutoResetEvent appServiceExit;
        private static ShellFolder recycler;

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
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                    if (arguments == "Terminate")
                    {
                        // Exit fulltrust process (UWP is closed or suspended)
                        appServiceExit.Set();
                        messageDeferral.Complete();
                    }
                    else if (arguments == "RecycleBin")
                    {
                        var action = (string)args.Request.Message["action"];
                        if (action == "Empty")
                        {
                            // Shell function to empty recyclebin
                            Vanara.PInvoke.Shell32.SHEmptyRecycleBin(IntPtr.Zero, null, Vanara.PInvoke.Shell32.SHERB.SHERB_NOCONFIRMATION | Vanara.PInvoke.Shell32.SHERB.SHERB_NOPROGRESSUI);
                        }
                        else if (action == "Query")
                        {
                            var responseQuery = new ValueSet();
                            Win32API.SHQUERYRBINFO queryBinInfo = new Win32API.SHQUERYRBINFO();
                            queryBinInfo.cbSize = (uint)Marshal.SizeOf(typeof(Win32API.SHQUERYRBINFO));
                            var res = Win32API.SHQueryRecycleBin("", ref queryBinInfo);
                            // TODO: use this when updated library is released
                            //Vanara.PInvoke.Shell32.SHQUERYRBINFO queryBinInfo = new Vanara.PInvoke.Shell32.SHQUERYRBINFO();
                            //Vanara.PInvoke.Shell32.SHQueryRecycleBin(null, ref queryBinInfo);
                            if (res == Vanara.PInvoke.HRESULT.S_OK)
                            {
                                var numItems = queryBinInfo.i64NumItems;
                                var binSize = queryBinInfo.i64Size;
                                responseQuery.Add("NumItems", numItems);
                                responseQuery.Add("BinSize", binSize);
                                await args.Request.SendResponseAsync(responseQuery);
                            }
                        }
                        else if (action == "Enumerate")
                        {
                            // Enumerate recyclebin contents and send response to UWP
                            var responseEnum = new ValueSet();
                            var folderContentsList = new List<ShellFileItem>();
                            foreach (var folderItem in recycler)
                            {
                                try
                                {
                                    folderItem.Properties.ReadOnly = true;
                                    folderItem.Properties.NoInheritedProperties = false;
                                    string recyclePath = folderItem.FileSystemPath; // True path on disk
                                    string fileName = Path.GetFileName(folderItem.Name); // Original file name
                                    string filePath = folderItem.Name; // Original file path + name
                                    var dt = (System.Runtime.InteropServices.ComTypes.FILETIME)folderItem.Properties[Vanara.PInvoke.Ole32.PROPERTYKEY.System.DateCreated];
                                    var recycleDate = dt.ToDateTime().ToLocalTime(); // This is LocalTime
                                    string fileSize = folderItem.Properties.GetPropertyString(Vanara.PInvoke.Ole32.PROPERTYKEY.System.Size);
                                    ulong fileSizeBytes = (ulong)folderItem.Properties[Vanara.PInvoke.Ole32.PROPERTYKEY.System.Size];
                                    string fileType = (string)folderItem.Properties[Vanara.PInvoke.Ole32.PROPERTYKEY.System.ItemTypeText];
                                    bool isFolder = folderItem.IsFolder && Path.GetExtension(folderItem.Name) != ".zip";
                                    folderContentsList.Add(new ShellFileItem(isFolder, recyclePath, fileName, filePath, recycleDate, fileSize, fileSizeBytes, fileType));
                                }
                                catch (System.IO.FileNotFoundException)
                                {
                                    // Happens if files are being deleted
                                }
                                finally
                                {
                                    folderItem.Dispose();
                                }
                            }
                            responseEnum.Add("Enumerate", Newtonsoft.Json.JsonConvert.SerializeObject(folderContentsList));
                            await args.Request.SendResponseAsync(responseEnum);
                        }
                    }
                    else if (arguments == "StartupTasks")
                    {
                        // Check QuickLook Availability
                        QuickLook.CheckQuickLookAvailability(localSettings);
                    }
                    else if (arguments == "ToggleQuickLook")
                    {
                        var path = (string)args.Request.Message["path"];
                        QuickLook.ToggleQuickLook(path);
                    }
                    else if (arguments == "ShellCommand")
                    {
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
                    }
                    else if (args.Request.Message.ContainsKey("Application"))
                    {
                        HandleApplicationLaunch(args);
                    }
                }
                else if (args.Request.Message.ContainsKey("Application"))
                {
                    HandleApplicationLaunch(args);
                }
            }
            finally
            {
                // Complete the deferral so that the platform knows that we're done responding to the app service call.
                // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
                messageDeferral.Complete();
            }
        }

        private static void HandleApplicationLaunch(AppServiceRequestReceivedEventArgs args)
        {
            var arguments = args.Request.Message.Get<string, string, object>("Arguments");

            try
            {
                var executable = (string)args.Request.Message["Application"];
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = executable;
                // Show window if arguments (opening terminal)
                process.StartInfo.CreateNoWindow = string.IsNullOrEmpty(arguments);
                process.StartInfo.Arguments = arguments;
                process.Start();
            }
            catch (Win32Exception)
            {
                var executable = (string)args.Request.Message["Application"];
                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas";
                process.StartInfo.FileName = executable;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Arguments = arguments;
                try
                {
                    process.Start();
                }
                catch (Win32Exception)
                {
                    try
                    {
                        Process.Start(executable);
                    }
                    catch (Win32Exception)
                    {
                        // Cannot open file (e.g DLL)
                    }
                }
            }
        }

        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            // Signal the event so the process can shut down
            appServiceExit.Set();
        }
    }
}
