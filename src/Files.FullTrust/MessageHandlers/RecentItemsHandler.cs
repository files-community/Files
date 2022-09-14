using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
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

        public Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, JsonElement> message, string arguments)
        {
            return Task.CompletedTask;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                quickAccessJumpListWatcher?.Dispose();
            }
        }
    }
}
