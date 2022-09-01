using Files.FullTrust.Helpers;
using Files.Shared;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Foundation.Collections;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class RecentItemsHandler : Disposable, IMessageHandler
    {
        private const string QuickAccessJumpListFileName = "5f7b5f1e01b83767.automaticDestinations-ms";
        private const string QuickAccessGuid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";
        private static string RecentItemsPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
        private static string AutomaticDestinationsPath = Path.Combine(RecentItemsPath, "AutomaticDestinations");

        private DateTime quickAccessLastReadTime = DateTime.MinValue;
        private FileSystemWatcher quickAccessJumpListWatcher;
        private PipeStream connection;

        public void Initialize(PipeStream connection)
        {
            this.connection = connection;

            StartQuickAccessJumpListWatcher();
        }

        /// <summary>
        /// Watch the quick access jump list file for any changes.
        /// Triggered by operations such as: added, accessed, removed from quick access, calls to SHAddToRecentDocs, etc..
        /// </summary>
        private void StartQuickAccessJumpListWatcher()
        {
            if (quickAccessJumpListWatcher is not null)
            {
                return;
            }

            quickAccessJumpListWatcher = new FileSystemWatcher
            {
                Path = AutomaticDestinationsPath,
                Filter = QuickAccessJumpListFileName,
                NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite,
            };
            quickAccessJumpListWatcher.Changed += QuickAccessJumpList_Changed;
            quickAccessJumpListWatcher.Deleted += QuickAccessJumpList_Changed;
            quickAccessJumpListWatcher.EnableRaisingEvents = true;
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "ShellRecentItems":
                    await HandleShellRecentItemsMessage(message);
                    break;
            }
        }

        private async void QuickAccessJumpList_Changed(object sender, FileSystemEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"{nameof(QuickAccessJumpList_Changed)}: {e.ChangeType}, {e.FullPath}");

            // skip if multiple events occurred for singular change
            var lastWriteTime = File.GetLastWriteTime(e.FullPath);
            if (quickAccessLastReadTime >= lastWriteTime)
            {
                return;
            }
            else
            {
                quickAccessLastReadTime = lastWriteTime;
            }

            if (connection?.IsConnected ?? false)
            {
                var response = new ValueSet()
                {
                    { "RecentItems", e.FullPath },
                    { "ChangeType", "QuickAccessJumpListChanged" },
                };

                // send message to UWP app to refresh recent files
                await Win32API.SendMessageAsync(connection, response);
            }
        }

        private async Task HandleShellRecentItemsMessage(Dictionary<string, object> message)
        {
            var action = (string)message["action"];
            var response = new ValueSet();

            switch (action)
            {
                // enumerate `\Windows\Recent` for recent folders
                // note: files are enumerated using (Win32MessageHandler: "ShellFolder") in RecentItemsManager
                case "EnumerateFolders":
                    var enumerateFoldersResponse = await Win32API.StartSTATask(() =>
                    {
                        try
                        {
                            var shellLinkItems = new List<ShellLinkItem>();
                            var excludeMask = FileAttributes.Hidden;
                            var linkFilePaths = Directory.EnumerateFiles(RecentItemsPath).Where(f => (new FileInfo(f).Attributes & excludeMask) == 0);
                            
                            foreach (var linkFilePath in linkFilePaths)
                            {
                                using var link = new ShellLink(linkFilePath, LinkResolution.NoUIWithMsgPump, null, TimeSpan.FromMilliseconds(100));

                                try
                                {
                                    if (!string.IsNullOrEmpty(link.TargetPath) && link.Target.IsFolder)
                                    {
                                        var shellLinkItem = ShellFolderExtensions.GetShellLinkItem(link);
                                        shellLinkItems.Add(shellLinkItem);
                                    }
                                }
                                catch (FileNotFoundException)
                                {
                                    // occurs when shortcut or shortcut target is deleted and accessed (link.Target)
                                    // consequently, we shouldn't include the item as a recent item
                                }
                            }

                            response.Add("EnumerateFolders", JsonConvert.SerializeObject(shellLinkItems));
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, enumerateFoldersResponse, message.Get("RequestID", (string)null));
                    break;

                case "Add":
                    var addResponse = await Win32API.StartSTATask(() =>
                    {
                        try
                        {
                            var path = (string) message["Path"];
                            Shell32.SHAddToRecentDocs(Shell32.SHARD.SHARD_PATHW, path);
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, addResponse, message.Get("RequestID", (string)null));
                    break;

                case "Clear":
                    var clearResponse = await Win32API.StartSTATask(() =>
                    {
                        try
                        {
                            Shell32.SHAddToRecentDocs(Shell32.SHARD.SHARD_PIDL, (string)null);
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, clearResponse, message.Get("RequestID", (string)null));
                    break;

                // invoke 'remove' verb on the file to remove it from Quick Access
                // note: for folders, we need to use the verb 'unpinfromhome' or 'removefromhome'
                case "UnpinFile":
                    var unpinFileResponse = await Win32API.StartSTATask(() =>
                    {
                        try
                        {
                            var path = (string)message["Path"];
                            var command = $"-command \"((New-Object -ComObject Shell.Application).Namespace('shell:{QuickAccessGuid}\').Items() " +
                                          $"| Where-Object {{ $_.Path -eq '{path}' }}).InvokeVerb('remove')\"";
                            bool success = Win32API.RunPowershellCommand(command, false);

                            response.Add("UnpinFile", path);
                            response.Add("Success", success);
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, unpinFileResponse, message.Get("RequestID", (string)null));
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                quickAccessJumpListWatcher?.Dispose();
            }
        }
    }
}
