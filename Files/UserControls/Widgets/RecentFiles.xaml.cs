using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Files
{
    public sealed partial class RecentFiles : UserControl
    {
        private ObservableCollection<RecentItem> recentItemsCollection = new ObservableCollection<RecentItem>();
        private EmptyRecentsText Empty { get; set; } = new EmptyRecentsText();
        public SettingsViewModel AppSettings => App.AppSettings;

        public RecentFiles()
        {
            InitializeComponent();

            recentItemsCollection.Clear();
            PopulateRecentsList();
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var flyoutItem = sender as MenuFlyoutItem;
            var clickedOnItem = flyoutItem.DataContext as RecentItem;
            if (clickedOnItem.IsFile)
            {
                var filePath = clickedOnItem.RecentPath;
                var folderPath = filePath.Substring(0, filePath.Length - clickedOnItem.Name.Length);
                App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), folderPath);
            }
        }

        public async void PopulateRecentsList()
        {
            var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
            bool IsRecentsListEmpty = true;
            foreach (var entry in mostRecentlyUsed.Entries)
            {
                try
                {
                    var item = await mostRecentlyUsed.GetItemAsync(entry.Token);
                    if (item.IsOfType(StorageItemTypes.File))
                    {
                        IsRecentsListEmpty = false;
                    }
                }
                catch (Exception) { }
            }

            if (IsRecentsListEmpty)
            {
                Empty.Visibility = Visibility.Visible;
            }
            else
            {
                Empty.Visibility = Visibility.Collapsed;
            }

            foreach (Windows.Storage.AccessCache.AccessListEntry entry in mostRecentlyUsed.Entries)
            {
                string mruToken = entry.Token;
                try
                {
                    IStorageItem item = await mostRecentlyUsed.GetItemAsync(mruToken);
                    await AddItemToRecentList(item, entry);
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip item until consent is provided
                }
                catch (Exception ex) when (
                    ex is COMException
                    || ex is FileNotFoundException
                    || ex is ArgumentException
                    || (uint)ex.HResult == 0x8007016A // The cloud file provider is not running
                    || (uint)ex.HResult == 0x8000000A) // The data necessary to complete this operation is not yet available
                {
                    mostRecentlyUsed.Remove(mruToken);
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            if (recentItemsCollection.Count == 0)
            {
                Empty.Visibility = Visibility.Visible;
            }
        }

        private async Task AddItemToRecentList(IStorageItem item, Windows.Storage.AccessCache.AccessListEntry entry)
        {
            BitmapImage ItemImage;
            string ItemPath;
            string ItemName;
            StorageItemTypes ItemType;
            Visibility ItemFolderImgVis;
            Visibility ItemEmptyImgVis;
            Visibility ItemFileIconVis;
            if (item.IsOfType(StorageItemTypes.File))
            {
                // Try to read the file to check if still exists
                // This is only needed to remove files opened from a disconnected android/MTP phone
                if (string.IsNullOrEmpty(item.Path)) // This indicates that the file was open from an MTP device
                {
                    using (var inputStream = await ((StorageFile)item).OpenReadAsync())
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
                StorageFile file = (StorageFile)item;
                var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 30, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale);
                if (thumbnail == null)
                {
                    ItemEmptyImgVis = Visibility.Visible;
                }
                else
                {
                    await ItemImage.SetSourceAsync(thumbnail.CloneStream());
                    ItemEmptyImgVis = Visibility.Collapsed;
                }
                ItemFolderImgVis = Visibility.Collapsed;
                ItemFileIconVis = Visibility.Visible;
                recentItemsCollection.Add(new RecentItem() { RecentPath = ItemPath, Name = ItemName, Type = ItemType, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
            }
        }

        private async void RecentsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var path = (e.ClickedItem as RecentItem).RecentPath;
            try
            {
                var directoryName = Path.GetDirectoryName(path);
                await Interaction.InvokeWin32Component(path, workingDir: directoryName);
            }
            catch (UnauthorizedAccessException)
            {
                var consentDialog = new ConsentDialog();
                await consentDialog.ShowAsync();
            }
            catch (ArgumentException)
            {
                if (new DirectoryInfo(path).Root.ToString().Contains(@"C:\"))
                {
                    App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), path);
                }
                else
                {
                    foreach (DriveItem drive in AppSettings.DrivesManager.Drives)
                    {
                        if (drive.Path.ToString() == new DirectoryInfo(path).Root.ToString())
                        {
                            App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), path);
                            return;
                        }
                    }
                }
            }
            catch (COMException)
            {
                await DialogDisplayHelper.ShowDialog(
                    "DriveUnpluggedDialog/Title".GetLocalized(),
                    "DriveUnpluggedDialog/Text".GetLocalized());
            }
        }

        private async void RemoveOneFrequentItem(object sender, RoutedEventArgs e)
        {
            // Get the sender frameworkelement

            if (sender is MenuFlyoutItem fe)
            {
                // Grab it's datacontext ViewModel and remove it from the list.

                if (fe.DataContext is RecentItem vm)
                {
                    if (await DialogDisplayHelper.ShowDialog("Remove item from Recents List", "Do you wish to remove " + vm.Name + " from the list?", "Yes", "No"))
                    {
                        // remove it from the visible collection
                        recentItemsCollection.Remove(vm);

                        // Now clear it also from the recent list cache permanently.
                        // No token stored in the viewmodel, so need to find it the old fashioned way.
                        var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

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
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            recentItemsCollection.Clear();
            RecentsView.ItemsSource = null;
            var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
            mru.Clear();
            Empty.Visibility = Visibility.Visible;
        }
    }

    public class RecentItem
    {
        public BitmapImage FileImg { get; set; }
        public string RecentPath { get; set; }
        public string Name { get; set; }
        public bool IsFile { get => Type == StorageItemTypes.File; }
        public StorageItemTypes Type { get; set; }
        public Visibility FolderImg { get; set; }
        public Visibility EmptyImgVis { get; set; }
        public Visibility FileIconVis { get; set; }
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