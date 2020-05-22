using Files.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace FilesFullTrust
{
    internal class Program
    {
        //Put all the variables required for the DLLImports here
        enum RecycleFlags : uint { SHERB_NOCONFIRMATION = 0x00000001, SHERB_NOPROGRESSUI = 0x00000002, SHERB_NOSOUND = 0x00000004 }

        [DllImport("Shell32.dll")]
        static extern int SHEmptyRecycleBin
              (IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        [STAThread]
        private static void Main(string[] args)
        {
            // Only one instance of the fulltrust process allowed
            // This happens if multiple instances of the UWP app are launched
            var mutex = new Mutex(true, "FilesUwpFullTrust", out bool isNew);
            if (!isNew) return;

            Console.WriteLine("*****************************");
            Console.WriteLine("**** Files UWP FullTrust ****");
            Console.WriteLine("*****************************");

            // Create shell COM object and get recycle bin folder
            shell = new Shell32.Shell();
            // Recycler = shell.NameSpace(ShellSpecialFolderConstants.ssfBITBUCKET);
            Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
            Recycler = (Shell32.Folder)shellAppType.InvokeMember("NameSpace",
                System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { Shell32.ShellSpecialFolderConstants.ssfBITBUCKET });

            // Save Localized recycle bin name
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["RecycleBin_Title"] = Recycler.Title;

            //for (int i = 0; i < 0xFFFF; i++)
            //{
            //string detail = Recycler.GetDetailsOf(null, i);
            //if (string.IsNullOrEmpty(detail)) break;
            //Console.WriteLine("[{0}]: {1}",i,detail);
            //}

            watchers = new List<FileSystemWatcher>();

            // Get user login SID
            // Recycle bin path is fixed on NTFS drives: X:\$Recycle.Bin\SID
            var sid = WindowsIdentity.GetCurrent().User;

            foreach (var drive in DriveInfo.GetDrives())
            {
                var recycle_path = Path.Combine(drive.Name, "$Recycle.Bin", sid.ToString());
                if (!Directory.Exists(recycle_path)) continue;

                // FileSystemWatcher does not work on shell folders
                // Get here the disk path of recycle bin for each drive
                var watcher = new FileSystemWatcher();
                watcher.Path = recycle_path;
                watcher.NotifyFilter = NotifyFilters.LastAccess
                                    | NotifyFilters.LastWrite
                                    | NotifyFilters.FileName
                                    | NotifyFilters.DirectoryName;

                // Watch all files
                watcher.Filter = "*.*";

                // Add event handlers.
                //watcher.Changed += Watcher_Changed;
                watcher.Created += Watcher_Changed;
                watcher.Deleted += Watcher_Changed;
                //watcher.Renamed += Watcher_Renamed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
            }

            // Connect to app service and wait until the connection gets closed
            appServiceExit = new AutoResetEvent(false);
            InitializeAppServiceConnection();
            appServiceExit.WaitOne();

            foreach (var watcher in watchers)
                watcher.Dispose();
            Marshal.FinalReleaseComObject(shell);
        }

        private static void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
        }

        private static async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
            if (connection != null)
            {
                // Send message to UWP app to refresh items
                await connection.SendMessageAsync(new ValueSet() { { "FileSystem", @"Shell:RecycleBinFolder" }, { "Path", e.FullPath }, { "Type", e.ChangeType.ToString() } });
            }
        }

        private static AppServiceConnection connection;
        private static AutoResetEvent appServiceExit;
        private static Shell32.Shell shell;
        private static Shell32.Folder Recycler;
        private static List<FileSystemWatcher> watchers;

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

                    if (arguments.Equals("Terminate"))
                    {
                        // Exit fulltrust process (UWP is closed or suspended)
                        appServiceExit.Set();
                        messageDeferral.Complete();
                    }
                    else if (arguments.Equals("RecycleBin"))
                    {
                        var action = (string)args.Request.Message["action"];
                        if (action.Equals("Empty"))
                        {
                            // Shell function to empty recyclebin
                            SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHERB_NOCONFIRMATION | RecycleFlags.SHERB_NOPROGRESSUI);
                        }
                        else if (action.Equals("Enumerate"))
                        {
                            // Enumerate recyclebin contents and send response to UWP
                            var response_enum = new ValueSet();
                            Shell32.FolderItems items = Recycler.Items();
                            var file_list = new List<ShellFileItem>();
                            for (int i = 0; i < items.Count; i++)
                            {
                                Shell32.FolderItem FI = items.Item(i);
                                string RecyclePath = FI.Path; // True path on disk
                                string FileName = Recycler.GetDetailsOf(FI, 0); // Original file name
                                string FilePath = Recycler.GetDetailsOf(FI, 1); // Original file path
                                FileInfo fileInfo = new FileInfo(RecyclePath);
                                DateTime RecycleDate = fileInfo.CreationTime; // Recycler.GetDetailsOf(FI, 2); Not easy to parse, depends on Locale
                                string FileSize = Recycler.GetDetailsOf(FI, 3);
                                int FileSizeBytes = FI.Size;
                                string FileType = Recycler.GetDetailsOf(FI, 4);
                                bool IsFolder = fileInfo.Attributes.HasFlag(FileAttributes.Directory); //FI.IsFolder includes .zip
                                file_list.Add(new ShellFileItem(IsFolder, RecyclePath, FileName, FilePath, RecycleDate, FileSize, FileSizeBytes, FileType));
                            }
                            response_enum.Add("Enumerate", Newtonsoft.Json.JsonConvert.SerializeObject(file_list));
                            await args.Request.SendResponseAsync(response_enum);
                        }
                    }
                    else if (arguments.Equals("StartupTasks"))
                    {
                        // Check QuickLook Availability
                        QuickLook.CheckQuickLookAvailability(localSettings);
                    }
                    else if (arguments.Equals("ToggleQuickLook"))
                    {
                        var path = (string)args.Request.Message["path"];
                        QuickLook.ToggleQuickLook(path);
                    }
                    else if (arguments.Equals("ShellCommand"))
                    {
                        // Kill the process. This is a BRUTAL WAY to kill a process.
                        var pid = (int)args.Request.Message["pid"];
                        Process.GetProcessById(pid).Kill();

                        Process process = new Process();
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.FileName = "explorer.exe";
                        process.StartInfo.CreateNoWindow = false;
                        process.StartInfo.Arguments = (string)args.Request.Message["ShellCommand"];
                        process.Start();
                    }
                    else if (args.Request.Message.ContainsKey("Application"))
                    {
                        var executable = (string)args.Request.Message["Application"];
                        Process process = new Process();
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.FileName = executable;
                        process.StartInfo.CreateNoWindow = false;
                        process.StartInfo.Arguments = arguments;
                        process.Start();
                    }
                }
                else if (args.Request.Message.ContainsKey("Application"))
                {
                    try
                    {
                        var executable = (string)args.Request.Message["Application"];
                        Process process = new Process();
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.FileName = executable;
                        process.StartInfo.CreateNoWindow = true;
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
            }
            finally
            {
                // Complete the deferral so that the platform knows that we're done responding to the app service call.
                // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
                messageDeferral.Complete();
            }
        }

        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            // Signal the event so the process can shut down
            appServiceExit.Set();
        }
    }
}
