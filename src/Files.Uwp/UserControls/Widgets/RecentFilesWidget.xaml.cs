﻿using Files.Shared.Enums;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using Files.Backend.Services.Settings;
using Files.Uwp.ViewModels;
using Files.Uwp.ViewModels.Widgets;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.UserControls.Widgets
{
    public sealed partial class RecentFilesWidget : UserControl, IWidgetItemModel
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public delegate void RecentFilesOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);

        public event RecentFilesOpenLocationInvokedEventHandler RecentFilesOpenLocationInvoked;

        public delegate void RecentFileInvokedEventHandler(object sender, PathNavigationEventArgs e);

        public event RecentFileInvokedEventHandler RecentFileInvoked;

        private ObservableCollection<RecentItem> recentItemsCollection = new ObservableCollection<RecentItem>();
        private EmptyRecentsText Empty { get; set; } = new EmptyRecentsText();

        public string WidgetName => nameof(RecentFilesWidget);

        public string AutomationProperties => "RecentFilesWidgetAutomationProperties/Name".GetLocalized();

        public string WidgetHeader => "RecentFiles".GetLocalized();

        public bool IsWidgetSettingEnabled => UserSettingsService.WidgetsSettingsService.ShowRecentFilesWidget;

        public RecentFilesWidget()
        {
            InitializeComponent();

            recentItemsCollection.Clear();

            _ = PopulateRecentsList();
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var flyoutItem = sender as MenuFlyoutItem;
            var clickedOnItem = flyoutItem.DataContext as RecentItem;
            if (clickedOnItem.IsFile)
            {
                var filePath = clickedOnItem.RecentPath;
                var folderPath = filePath.Substring(0, filePath.Length - clickedOnItem.Name.Length);
                RecentFilesOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
                {
                    ItemPath = folderPath,
                    ItemName = clickedOnItem.Name
                });
            }
        }

        private async Task PopulateRecentsList()
        {
            Empty.Visibility = Visibility.Collapsed;

            try
            {
                var mostRecentlyUsed = StorageApplicationPermissions.MostRecentlyUsedList;

                foreach (AccessListEntry entry in mostRecentlyUsed.Entries)
                {
                    string mruToken = entry.Token;
                    var added = await FilesystemTasks.Wrap(async () =>
                    {
                        IStorageItem item = await mostRecentlyUsed.GetItemAsync(mruToken, AccessCacheOptions.FastLocationsOnly | AccessCacheOptions.SuppressAccessTimeUpdate);
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

            if (recentItemsCollection.Count == 0)
            {
                Empty.Visibility = Visibility.Visible;
            }
        }

        private async Task AddItemToRecentListAsync(IStorageItem item, AccessListEntry entry)
        {
            BitmapImage ItemImage;
            string ItemPath;
            string ItemName;
            StorageItemTypes ItemType;
            bool ItemFolderImgVis;
            bool ItemEmptyImgVis;
            bool ItemFileIconVis;
            if (item.IsOfType(StorageItemTypes.File))
            {
                // Try to read the file to check if still exists
                // This is only needed to remove files opened from a disconnected android/MTP phone
                if (string.IsNullOrEmpty(item.Path)) // This indicates that the file was open from an MTP device
                {
                    using (var inputStream = await item.AsBaseStorageFile().OpenReadAsync())
                    using (var classicStream = inputStream.AsStreamForRead())
                    using (var streamReader = new StreamReader(classicStream))
                    {
                        // NB: this might trigger the download of the file from OneDrive
                        streamReader.Peek();
                    }
                }

                ItemName = item.Name;
                ItemPath = string.IsNullOrEmpty(item.Path) ? entry.Metadata : item.Path;
                ItemType = StorageItemTypes.File;
                ItemImage = new BitmapImage();
                BaseStorageFile file = item.AsBaseStorageFile();
                using var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.ListView, 24, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale);
                if (thumbnail == null)
                {
                    ItemEmptyImgVis = true;
                }
                else
                {
                    await ItemImage.SetSourceAsync(thumbnail);
                    ItemEmptyImgVis = false;
                }
                ItemFolderImgVis = false;
                ItemFileIconVis = true;
                recentItemsCollection.Add(new RecentItem()
                {
                    RecentPath = ItemPath,
                    Name = ItemName,
                    Type = ItemType,
                    FolderImg = ItemFolderImgVis,
                    EmptyImgVis = ItemEmptyImgVis,
                    FileImg = ItemImage,
                    FileIconVis = ItemFileIconVis
                });
            }
        }

        private void RecentsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var path = (e.ClickedItem as RecentItem).RecentPath;
            RecentFileInvoked?.Invoke(this, new PathNavigationEventArgs()
            {
                ItemPath = path
            });
        }

        private async void RemoveRecentItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the sender frameworkelement

            if (sender is MenuFlyoutItem fe)
            {
                // Grab it's datacontext ViewModel and remove it from the list.

                if (fe.DataContext is RecentItem vm)
                {
                    // Remove it from the visible collection
                    recentItemsCollection.Remove(vm);

                    // Now clear it from the recent list cache permanently.
                    // No token stored in the viewmodel, so need to find it the old fashioned way.
                    var mru = StorageApplicationPermissions.MostRecentlyUsedList;

                    foreach (var element in mru.Entries)
                    {
                        var f = await mru.GetItemAsync(element.Token);
                        if (f.Path == vm.RecentPath || element.Metadata == vm.RecentPath)
                        {
                            mru.Remove(element.Token);
                            if (recentItemsCollection.Count == 0)
                            {
                                Empty.Visibility = Visibility.Visible;
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void ClearRecentItems_Click(object sender, RoutedEventArgs e)
        {
            recentItemsCollection.Clear();
            RecentsView.ItemsSource = null;
            var mru = StorageApplicationPermissions.MostRecentlyUsedList;
            mru.Clear();
            Empty.Visibility = Visibility.Visible;
        }

        public async Task RefreshWidget()
        {
            recentItemsCollection.Clear();
            await PopulateRecentsList();
        }

        public void Dispose()
        {
        }
    }

    public class RecentItem
    {
        public BitmapImage FileImg { get; set; }
        public string RecentPath { get; set; }
        public string Name { get; set; }
        public bool IsFile { get => Type == StorageItemTypes.File; }
        public StorageItemTypes Type { get; set; }
        public bool FolderImg { get; set; }
        public bool EmptyImgVis { get; set; }
        public bool FileIconVis { get; set; }
    }

    public class EmptyRecentsText : INotifyPropertyChanged
    {
        private Visibility visibility;

        public Visibility Visibility
        {
            get
            {
                return visibility;
            }
            set
            {
                if (value != visibility)
                {
                    visibility = value;
                    NotifyPropertyChanged(nameof(Visibility));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}