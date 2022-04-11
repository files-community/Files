using CommunityToolkit.Authentication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Graph;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Shared.Enums;
using Files.Uwp.Helpers.ListedItems;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Widgets.RecentFiles
{
    public class RecentFilesWidgetViewModel : ObservableObject, IDisposable
    {
        private IGraphRecentFilesService recentFilesService;

        private bool isRecentsListEmpty;

        public bool IsRecentsListEmpty
        {
            get => isRecentsListEmpty;
            set => SetProperty(ref isRecentsListEmpty, value);
        }

        public ObservableCollection<ListedItem> Items;

        public RecentFilesWidgetViewModel()
        {
            recentFilesService = Ioc.Default.GetService<IGraphRecentFilesService>();
            Items = new ObservableCollection<ListedItem>();
        }

        public async Task PopulateRecentsList()
        {
            IsRecentsListEmpty = false;

            try
            {
                if (ProviderManager.Instance.State != ProviderState.SignedIn)
                {
                    ProviderManager.Instance.ProviderStateChanged -= Instance_ProviderStateChanged;
                    ProviderManager.Instance.ProviderStateChanged += Instance_ProviderStateChanged;
                }
                else
                {
                    await GetRecentFilesFromCloud();
                }

                var mostRecentlyUsed = StorageApplicationPermissions.MostRecentlyUsedList;

                foreach (AccessListEntry entry in mostRecentlyUsed.Entries)
                {
                    string mruToken = entry.Token;
                    var added = await FilesystemTasks.Wrap(async () =>
                    {
                        IStorageItem item = await mostRecentlyUsed.GetItemAsync(mruToken, AccessCacheOptions.FastLocationsOnly);
                        await AddItemToRecentListAsync(item, entry);
                    });
                    if (added == FileSystemStatusCode.Unauthorized)
                    {
                        // Skip item until consent is provided
                    }
                    // Exceptions include but are not limited to:
                    // COMException, FileNotFoundException, ArgumentException, DirectoryNotFoundException
                    // 0x8007016A -> The cloud file provider is not running
                    // 0x8000000A -> The data necessary to complete this operation is not yet available
                    // 0x80004005 -> Unspecified error
                    // 0x80270301 -> ?
                    else if (!added)
                    {
                        await FilesystemTasks.Wrap(() =>
                        {
                            mostRecentlyUsed.Remove(mruToken);
                            return Task.CompletedTask;
                        });
                        System.Diagnostics.Debug.WriteLine(added.ErrorCode);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Info(ex, "Could not fetch recent items");
            }

            if (!Items.Any())
            {
                IsRecentsListEmpty = true;
            }
        }

        private async void Instance_ProviderStateChanged(object sender, ProviderStateChangedEventArgs e)
        {
            if (e.NewState == ProviderState.SignedIn)
            {
                ProviderManager.Instance.ProviderStateChanged -= Instance_ProviderStateChanged;
                await GetRecentFilesFromCloud();
            }
        }

        private async Task GetRecentFilesFromCloud()
        {
            var recentGraphItemsPage = await recentFilesService.GetRecentDriveItemsAsync();
            foreach (DriveItem recentItem in recentGraphItemsPage.Take(6))
            {
                AddGraphItemToRecentList(recentItem);
            }
        }

        private void AddGraphItemToRecentList(DriveItem recentGraphItem)
        {
            var thumbnail = new BitmapImage(new Uri(recentGraphItem.Thumbnails.First().Small.Url));
            thumbnail.DecodePixelHeight = 96;
            thumbnail.DecodePixelWidth = 96;

            Items.Add(new ListedItem()
            {
                ItemNameRaw = recentGraphItem.Name,
                ItemPath = recentGraphItem.WebUrl,
                ItemDateModifiedReal = recentGraphItem.LastModifiedDateTime,
                ItemDateCreatedReal = recentGraphItem.CreatedDateTime,
                FileSizeBytes = recentGraphItem.Size,
                FileImage = thumbnail
            });
        }

        private async Task AddItemToRecentListAsync(IStorageItem item, AccessListEntry entry)
        {
            if (item.IsOfType(StorageItemTypes.File))
            {
                // Read the file to check if still exists
                // This is only needed to remove files opened from a disconnected android/MTP phone
                if (string.IsNullOrEmpty(item.Path)) // Indicates that the file was open from an MTP device
                {
                    using (var inputStream = await item.AsBaseStorageFile().OpenReadAsync())
                    using (var classicStream = inputStream.AsStreamForRead())
                    using (var streamReader = new StreamReader(classicStream))
                    {
                        // Try to trigger the download of the file from OneDrive
                        streamReader.Peek();
                    }
                }

                BaseStorageFile file = item.AsBaseStorageFile();
                var listedItem = await ListedItemHelpers.AddFileAsync(file, getMinimalPropertySet: true);

                var bitmapImage = new BitmapImage();
                using var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.ListView, 24, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale);
                if (thumbnail != null)
                {
                    await bitmapImage.SetSourceAsync(thumbnail);
                    listedItem.FileImage = bitmapImage;
                }

                if (string.IsNullOrWhiteSpace(listedItem.ItemPath))
                {
                    listedItem.ItemPath = entry.Metadata;
                }

                Items.Add(listedItem);
            }
        }

        public void Dispose()
        {

        }
    }
}
