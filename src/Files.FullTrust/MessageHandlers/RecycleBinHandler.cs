using Files.Shared.Extensions;
using Files.FullTrust.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class RecycleBinHandler : Disposable, IMessageHandler
    {
        private IList<FileSystemWatcher> binWatchers;
        private PipeStream connection;

        public void Initialize(PipeStream connection)
        {
            this.connection = connection;

            // Create shell COM object and get recycle bin folder
            using var recycler = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_RecycleBinFolder);
            ApplicationData.Current.LocalSettings.Values["RecycleBin_Title"] = recycler.Name;

            StartRecycleBinWatcher();
        }

        private void StartRecycleBinWatcher()
        {
            // Create filesystem watcher to monitor recycle bin folder(s)
            // SHChangeNotifyRegister only works if recycle bin is open in explorer :(
            binWatchers = new List<FileSystemWatcher>();
            var sid = WindowsIdentity.GetCurrent().User.ToString();
            foreach (var drive in DriveInfo.GetDrives())
            {
                var recyclePath = Path.Combine(drive.Name, "$RECYCLE.BIN", sid);
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
                watcher.Created += RecycleBinWatcher_Changed;
                watcher.Deleted += RecycleBinWatcher_Changed;
                watcher.EnableRaisingEvents = true;
                binWatchers.Add(watcher);
            }
        }

        public Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            return Task.CompletedTask;
        }

        private async void RecycleBinWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Recycle bin event: {e.ChangeType}, {e.FullPath}");
            if (e.Name.StartsWith("$I", StringComparison.Ordinal))
            {
                // Recycle bin also stores a file starting with $I for each item
                return;
            }
            if (connection?.IsConnected ?? false)
            {
                var response = new ValueSet()
                {
                    { "FileSystem", @"Shell:RecycleBinFolder" },
                    { "Path", e.FullPath },
                    { "Type", e.ChangeType.ToString() }
                };
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    using var folderItem = SafetyExtensions.IgnoreExceptions(() => new ShellItem(e.FullPath));
                    if (folderItem == null) return;
                    var shellFileItem = ShellFolderExtensions.GetShellFileItem(folderItem);
                    response["Item"] = JsonSerializer.Serialize(shellFileItem);
                }
                // Send message to UWP app to refresh items
                await Win32API.SendMessageAsync(connection, response);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var watcher in binWatchers)
                {
                    watcher.Dispose();
                }
            }
        }
    }
}
