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

        private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}", JsonContext.Default.String);

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
                    response["Item"] = JsonSerializer.Serialize(ShellFolderExtensions.GetShellLibraryItem(library, newPath), JsonContext.Default.ShellLibraryItem);
                    library.Dispose();
                }
                // Send message to UWP app to refresh items
                await Win32API.SendMessageAsync(connection, response);
            }
        }

        private async Task HandleShellLibraryMessage(Dictionary<string, JsonElement> message)
        {
            switch (message["action"].GetString())
            {
                case "Enumerate":
                    // Read library information and send response to UWP
                    var enumerateResponse = await Win32API.StartSTATask(() =>
                    {
                        var response = new ValueSet();
                        try
                        {
                            var libraryItems = new List<ShellLibraryItem>();
                            // https://docs.microsoft.com/en-us/windows/win32/search/-search-win7-development-scenarios#library-descriptions
                            var libFiles = Directory.EnumerateFiles(ShellLibraryItem.LibrariesPath, "*" + ShellLibraryItem.EXTENSION);
                            foreach (var libFile in libFiles)
                            {
                                using var shellItem = new ShellLibrary2(Shell32.ShellUtil.GetShellItemForPath(libFile), true);
                                if (shellItem is ShellLibrary2 library)
                                {
                                    libraryItems.Add(ShellFolderExtensions.GetShellLibraryItem(library, libFile));
                                }
                            }
                            response.Add("Enumerate", JsonSerializer.Serialize(libraryItems, JsonContext.Default.ListShellLibraryItem));
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, enumerateResponse, message.Get("RequestID", defaultJson).GetString());
                    break;

                case "Create":
                    // Try create new library with the specified name and send response to UWP
                    var createResponse = await Win32API.StartSTATask(() =>
                    {
                        var response = new ValueSet();
                        try
                        {
                            using var library = new ShellLibrary2(message["library"].GetString(), Shell32.KNOWNFOLDERID.FOLDERID_Libraries, false);
                            library.Folders.Add(ShellItem.Open(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))); // Add default folder so it's not empty
                            library.Commit();
                            library.Reload();
                            response.Add("Create", JsonSerializer.Serialize(ShellFolderExtensions.GetShellLibraryItem(library, library.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing)), JsonContext.Default.ShellLibraryItem));
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, createResponse, message.Get("RequestID", defaultJson).GetString());
                    break;

                case "Update":
                    // Update details of the specified library and send response to UWP
                    var updateResponse = await Win32API.StartSTATask(() =>
                    {
                        var response = new ValueSet();
                        try
                        {
                            var folders = message.ContainsKey("folders") ? JsonSerializer.Deserialize<string[]>(message["folders"].GetString(), JsonContext.Default.StringArray) : null;
                            var defaultSaveFolder = message.Get("defaultSaveFolder", defaultJson).GetString();
                            var isPinned = message.Get("isPinned", defaultJson).GetBoolean();

                            bool updated = false;
                            var libPath = message["library"].GetString();
                            using var library = new ShellLibrary2(Shell32.ShellUtil.GetShellItemForPath(libPath), false);
                            if (folders != null)
                            {
                                if (folders.Length > 0)
                                {
                                    var foldersToRemove = library.Folders.Where(f => !folders.Any(folderPath => string.Equals(folderPath, f.FileSystemPath, StringComparison.OrdinalIgnoreCase)));
                                    foreach (var toRemove in foldersToRemove)
                                    {
                                        library.Folders.Remove(toRemove);
                                        updated = true;
                                    }
                                    var foldersToAdd = folders.Distinct(StringComparer.OrdinalIgnoreCase)
                                                              .Where(folderPath => !library.Folders.Any(f => string.Equals(folderPath, f.FileSystemPath, StringComparison.OrdinalIgnoreCase)))
                                                              .Select(ShellItem.Open);
                                    foreach (var toAdd in foldersToAdd)
                                    {
                                        library.Folders.Add(toAdd);
                                        updated = true;
                                    }
                                    foreach (var toAdd in foldersToAdd)
                                    {
                                        toAdd.Dispose();
                                    }
                                }
                            }
                            if (defaultSaveFolder != null)
                            {
                                library.DefaultSaveFolder = ShellItem.Open(defaultSaveFolder);
                                updated = true;
                            }
                            library.PinnedToNavigationPane = isPinned == true;
                            updated = true;
                            if (updated)
                            {
                                library.Commit();
                                library.Reload(); // Reload folders list
                                response.Add("Update", JsonSerializer.Serialize(ShellFolderExtensions.GetShellLibraryItem(library, libPath), JsonContext.Default.ShellLibraryItem));
                            }
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, updateResponse, message.Get("RequestID", defaultJson).GetString());
                    break;
            }
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
