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

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class LibrariesHandler : Disposable, IMessageHandler
    {
        private PipeStream connection;

        private FileSystemWatcher librariesWatcher;

        private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

        public void Initialize(PipeStream connection)
        {
            this.connection = connection;

            StartLibrariesWatcher();
        }

        private void StartLibrariesWatcher()
        {
            librariesWatcher = new FileSystemWatcher
            {
                Path = ShellLibraryItem.LibrariesPath,
                Filter = "*" + ShellLibraryItem.EXTENSION,
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = false,
            };

            librariesWatcher.Created += (object _, FileSystemEventArgs e) => OnLibraryChanged(e.ChangeType, e.FullPath, e.FullPath);
            librariesWatcher.Changed += (object _, FileSystemEventArgs e) => OnLibraryChanged(e.ChangeType, e.FullPath, e.FullPath);
            librariesWatcher.Deleted += (object _, FileSystemEventArgs e) => OnLibraryChanged(e.ChangeType, e.FullPath, null);
            librariesWatcher.Renamed += (object _, RenamedEventArgs e) => OnLibraryChanged(e.ChangeType, e.OldFullPath, e.FullPath);
            librariesWatcher.EnableRaisingEvents = true;
        }

        public Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, JsonElement> message, string arguments)
            => arguments switch
            {
                "ShellLibrary" => HandleShellLibraryMessage(message),
                _ => Task.CompletedTask,
            };

        private async void OnLibraryChanged(WatcherChangeTypes changeType, string oldPath, string newPath)
        {
            if (newPath != null && (!newPath.ToLowerInvariant().EndsWith(ShellLibraryItem.EXTENSION, StringComparison.Ordinal) || !File.Exists(newPath)))
            {
                System.Diagnostics.Debug.WriteLine($"Ignored library event: {changeType}, {oldPath} -> {newPath}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Library event: {changeType}, {oldPath} -> {newPath}");

            if (connection?.IsConnected ?? false)
            {
                var response = new ValueSet { { "Library", newPath ?? oldPath } };
                switch (changeType)
                {
                    case WatcherChangeTypes.Deleted:
                    case WatcherChangeTypes.Renamed:
                        response["OldPath"] = oldPath;
                        break;

                    default:
                        break;
                }
                if (!changeType.HasFlag(WatcherChangeTypes.Deleted))
                {
                    var library = SafetyExtensions.IgnoreExceptions(() => new ShellLibrary2(Shell32.ShellUtil.GetShellItemForPath(newPath), true));
                    if (library == null)
                    {
                        Program.Logger.Warn($"Failed to open library after {changeType}: {newPath}");
                        return;
                    }
                    response["Item"] = JsonSerializer.Serialize(ShellFolderExtensions.GetShellLibraryItem(library, newPath));
                    library.Dispose();
                }
                // Send message to UWP app to refresh items
                await Win32API.SendMessageAsync(connection, response);
            }
        }

        private async Task HandleShellLibraryMessage(Dictionary<string, JsonElement> message)
        {
            
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                librariesWatcher?.Dispose();
            }
        }
    }
}
