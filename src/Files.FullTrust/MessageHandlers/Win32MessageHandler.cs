using Files.Shared;
using Files.Shared.Extensions;
using Files.FullTrust.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class Win32MessageHandler : Disposable, IMessageHandler
    {
        private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");
        private IList<FileSystemWatcher> dirWatchers;
        private PipeStream connection;

        public Win32MessageHandler()
        {
            dirWatchers = new List<FileSystemWatcher>();
        }

        public void Initialize(PipeStream connection)
        {
            this.connection = connection;
            ApplicationData.Current.LocalSettings.Values["TEMP"] = Environment.GetEnvironmentVariable("TEMP");
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, JsonElement> message, string arguments)
        {
            switch (arguments)
            {
                case "WatchDirectory":
                    var watchAction = message["action"].GetString();
                    await ParseWatchDirectoryActionAsync(connection, message, watchAction);
                    break;
            }
        }

        private async Task ParseWatchDirectoryActionAsync(PipeStream connection, Dictionary<string, JsonElement> message, string action)
        {
            switch (action)
            {
                case "start":
                    {
                        var res = new ValueSet();
                        var folderPath = message["folderPath"].GetString();
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
                        await Win32API.SendMessageAsync(connection, res, message.Get("RequestID", defaultJson).GetString());
                    }
                    break;

                case "cancel":
                    {
                        var watcherID = message["watcherID"].GetInt64();
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
                    response["Item"] = JsonSerializer.Serialize(shellFileItem);
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
            }
        }
    }
}
