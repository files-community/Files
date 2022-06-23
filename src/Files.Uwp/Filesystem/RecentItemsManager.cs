using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared;
using Files.Shared.Enums;
using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.Filesystem
{
    public class RecentItemsManager : IDisposable
    {
        private readonly ILogger logger = Ioc.Default.GetService<ILogger>();
        private const string QuickAccessGuid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";

        public EventHandler<NotifyCollectionChangedEventArgs> RecentFilesChanged;
        public EventHandler<NotifyCollectionChangedEventArgs> RecentFoldersChanged;

        // recent files
        private readonly List<RecentItem> recentFiles = new();
        public IReadOnlyList<RecentItem> RecentFiles    // already sorted
        {
            get
            {
                lock (recentFiles)
                {
                    return recentFiles.ToList().AsReadOnly();
                }
            }
        }

        // recent folders
        private readonly List<RecentItem> recentFolders = new();
        public IReadOnlyList<RecentItem> RecentFolders  // already sorted
        {
            get
            {
                lock (recentFolders)
                {
                    return recentFolders.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Refetch recent files to `recentFiles`.
        /// </summary>
        public async Task UpdateRecentFilesAsync()
        {
            // enumerate with fulltrust process
            List<RecentItem> enumeratedFiles = await ListRecentFilesAsync();
            if (enumeratedFiles != null)
            {
                lock (recentFiles)
                {
                    recentFiles.Clear();
                    recentFiles.AddRange(enumeratedFiles);
                    // do not sort here, enumeration order *is* the correct order since we get it from Quick Access
                }

                // todo: potentially optimize this and figure out if list changed by either (1) Add (2) Remove (3) Move
                // this way the UI doesn't have to refresh the entire list everytime a change occurs
                RecentFilesChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Refetch recent folders to `recentFolders`.
        /// </summary>
        public async Task UpdateRecentFoldersAsync()
        {
            // enumerate with fulltrust process
            List<RecentItem> enumeratedFolders = await ListRecentFoldersAsync();
            if (enumeratedFolders != null)
            {
                lock (recentFolders)
                {
                    recentFolders.Clear();
                    recentFolders.AddRange(enumeratedFolders);

                    // shortcut modifications in `Windows\Recent` consist of a delete + add operation;
                    // thus, last modify date is reset and we can sort off it
                    recentFolders.Sort((x, y) => y.LastModified.CompareTo(x.LastModified));
                }

                RecentFoldersChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Enumerate recently accessed files via `Quick Access`.
        /// </summary>
        public async Task<List<RecentItem>> ListRecentFilesAsync()
        {
            var connection = await AppServiceConnectionHelper.Instance;

            if (connection != null)
            {
                // enumerate the Quick Access shell virtual path directly (handled via Win32MessageHandler)
                ValueSet value = new ValueSet()
                {
                    { "Arguments", "ShellFolder" },
                    { "action", "Enumerate" },
                    { "folder", QuickAccessGuid }
                };
                var (status, response) = await connection.SendMessageForResponseAsync(value);

                if (status == AppServiceResponseStatus.Success && response.ContainsKey("Enumerate"))
                {
                    var items = JsonConvert.DeserializeObject<List<ShellFileItem>>((string)response["Enumerate"])
                                           .Select(link => new RecentItem(link)).ToList();
                    return items;
                }
            }

            return new();
        }

        /// <summary>
        /// Enumerate recently accessed folders via `Windows\Recent`.
        /// </summary>
        public async Task<List<RecentItem>> ListRecentFoldersAsync()
        {
            List<RecentItem> linkItems = null;

            var (status, response) = await SendRecentItemsActionForResponse("EnumerateFolders");
            if (status == AppServiceResponseStatus.Success && response.ContainsKey("EnumerateFolders"))
            {
                linkItems = JsonConvert.DeserializeObject<List<ShellLinkItem>>((string) response["EnumerateFolders"])
                                       .Select(link => new RecentItem(link)).ToList();
            }

            return linkItems;
        }

        /// <summary>
        /// Adds a shortcut to `Windows\Recent`. The path can be to a file or folder.
        /// It will update to `recentFiles` or `recentFolders` respectively.
        /// </summary>
        /// <param name="path">Path to a file or folder</param>
        /// <returns>Whether the action was successfully handled or not</returns>
        public async Task<bool> AddToRecentItems(string path)
        {
            var (status, _) = await SendRecentItemsActionForResponse("Add", new ValueSet 
            { 
                { "Path", path },
            });
            return status == AppServiceResponseStatus.Success;
        }

        /// <summary>
        /// Clears both `recentFiles` and `recentFolders`.
        /// This will also clear the Recent Files (and its jumplist) in File Explorer.
        /// </summary>
        /// <returns>Whether the action was successfully handled or not</returns>
        public async Task<bool> ClearRecentItems()
        {
            var (status, _) = await SendRecentItemsActionForResponse("Clear");
            return status == AppServiceResponseStatus.Success;
        }

        /// <summary>
        /// Unpin (or remove) a file from `recentFiles`.
        /// This will also unpin the item from the Recent Files in File Explorer.
        /// </summary>
        /// <returns>Whether the action was successfully handled or not</returns>
        public async Task<bool> UnpinFromRecentFiles(string path)
        {
            var (status, response) = await SendRecentItemsActionForResponse("UnpinFile", new ValueSet
            {
                { "Path", path },
            });
            return status == AppServiceResponseStatus.Success &&
                   response.TryGetValue("Success", out var success) &&
                   (bool)success;
        }

        /// <summary>
        /// Send an action for response with the argument `ShellRecentItems` which is handled by RecentItemsHandler.
        /// </summary>
        /// <param name="actionValue">The action to perform (e.g. "EnumerateFolders")</param>
        /// <param name="extras">Any extra payload data needed (e.g. sending a path to enumerate)</param>
        /// <returns>A tuple containing the response status and any additional payload data as key-value pairs</returns>
        private async Task<(AppServiceResponseStatus Status, Dictionary<string, object> Data)> SendRecentItemsActionForResponse(string actionValue, ValueSet extras = null)
        {
            var connection = await AppServiceConnectionHelper.Instance;

            if (connection == null)
            {
                return (AppServiceResponseStatus.Failure, null);
            }

            var valueSet = new ValueSet
            {
                { "Arguments", "ShellRecentItems" },
                { "action", actionValue },
            };

            if (extras is not null)
            {
                foreach(var entry in extras)
                {
                    if (!valueSet.ContainsKey(entry.Key))
                    {
                        valueSet.Add(entry);
                    }
                }
            }

            return await connection.SendMessageForResponseAsync(valueSet);
        }

        /// <summary>
        /// Handle any events received from the fulltrust process.
        /// Events are only received when the user is on the home page (YourHomeViewModel is loaded).
        /// </summary>
        public async Task HandleWin32RecentItemsEvent(string changeType)
        {
            System.Diagnostics.Debug.WriteLine(nameof(HandleWin32RecentItemsEvent) + $": ({changeType})");

            switch (changeType)
            {
                case "QuickAccessJumpListChanged":
                    await UpdateRecentFilesAsync();
                    break;

                default:
                    logger.Warn($"{nameof(HandleWin32RecentItemsEvent)}: Received invalid changeType of {changeType}");
                    break;
            }
        }

        /// <summary>
        /// Returns whether two RecentItem enumerables have the same order.
        /// This function depends on `RecentItem` implementing IEquatable.
        /// </summary>
        private bool RecentItemsOrderEquals(IEnumerable<RecentItem> oldOrder, IEnumerable<RecentItem> newOrder)
        {
            if (oldOrder is null || newOrder is null)
            {
                return false;
            }

            return oldOrder.SequenceEqual(newOrder);
        }

        public void Dispose() {}
    }
}