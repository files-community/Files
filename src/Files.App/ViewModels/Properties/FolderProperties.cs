using ByteSizeLib;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using CommunityToolkit.WinUI;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Microsoft.UI.Dispatching;

namespace Files.App.ViewModels.Properties
{
    internal class FolderProperties : BaseProperties
    {
        public ListedItem Item { get; }

        public FolderProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource,
            DispatcherQueue coreDispatcher, ListedItem item, IShellPage instance)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
            Item = item;
            AppInstance = instance;

            GetBaseProperties();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        public override void GetBaseProperties()
        {
            if (Item != null)
            {
                ViewModel.ItemName = Item.Name;
                ViewModel.OriginalItemName = Item.Name;
                ViewModel.ItemType = Item.ItemType;
                ViewModel.ItemPath = (Item as RecycleBinItem)?.ItemOriginalFolder ??
                    (Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath);
                ViewModel.ItemModifiedTimestamp = Item.ItemDateModified;
                ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
                ViewModel.LoadCustomIcon = Item.LoadCustomIcon;
                ViewModel.CustomIconSource = Item.CustomIconSource;
                ViewModel.LoadFileIcon = Item.LoadFileIcon;
                ViewModel.ContainsFilesOrFolders = Item.ContainsFilesOrFolders;

                if (Item.IsShortcut)
                {
                    var shortcutItem = (ShortcutItem)Item;
                    ViewModel.ShortcutItemType = "Folder".GetLocalizedResource();
                    ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
                    ViewModel.IsShortcutItemPathReadOnly = false;
                    ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
                    ViewModel.ShortcutItemWorkingDirVisibility = false;
                    ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
                    ViewModel.ShortcutItemArgumentsVisibility = false;
                    ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
                    {
                        await App.Window.DispatcherQueue.EnqueueAsync(
                            () => NavigationHelpers.OpenPathInNewTab(Path.GetDirectoryName(ViewModel.ShortcutItemPath)));
                    }, () =>
                    {
                        return !string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath);
                    });
                }
            }
        }

        public async override void GetSpecialProperties()
        {
            ViewModel.IsHidden = NativeFileOperationsHelper.HasFileAttribute(
                Item.ItemPath, System.IO.FileAttributes.Hidden);

            var fileIconData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.ItemPath, 80, Windows.Storage.FileProperties.ThumbnailMode.SingleItem, true);
            if (fileIconData != null)
            {
                ViewModel.IconData = fileIconData;
                ViewModel.LoadFolderGlyph = false;
                ViewModel.LoadFileIcon = true;
            }

            if (Item.IsShortcut)
            {
                ViewModel.ItemSizeVisibility = true;
                ViewModel.ItemSize = Item.FileSizeBytes.ToLongSizeString();
                ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
                ViewModel.ItemAccessedTimestamp = Item.ItemDateAccessed;
                if (Item.IsLinkItem || string.IsNullOrWhiteSpace(((ShortcutItem)Item).TargetPath))
                {
                    // Can't show any other property
                    return;
                }
            }

            string folderPath = (Item as ShortcutItem)?.TargetPath ?? Item.ItemPath;
            BaseStorageFolder storageFolder = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(folderPath);

            if (storageFolder != null)
            {
                ViewModel.ItemCreatedTimestamp = dateTimeFormatter.ToShortLabel(storageFolder.DateCreated);
                if (storageFolder.Properties != null)
                {
                    GetOtherProperties(storageFolder.Properties);
                }
                GetFolderSize(storageFolder.Path, TokenSource.Token);
            }
            else if (Item.ItemPath.Equals(CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
            {
                // GetFolderFromPathAsync cannot access recyclebin folder
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    var value = new ValueSet();
                    value.Add("Arguments", "RecycleBin");
                    value.Add("action", "Query");
                    // Send request to fulltrust process to get recyclebin properties
                    var (status, response) = await connection.SendMessageForResponseAsync(value);
                    if (status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
                    {
                        if (response.TryGetValue("BinSize", out var binSize))
                        {
                            ViewModel.ItemSizeBytes = binSize.GetInt64();
                            ViewModel.ItemSize = ByteSize.FromBytes(binSize.GetInt64()).ToString();
                            ViewModel.ItemSizeVisibility = true;
                        }
                        else
                        {
                            ViewModel.ItemSizeVisibility = false;
                        }
                        if (response.TryGetValue("NumItems", out var numItems))
                        {
                            ViewModel.FilesCount = (int)numItems.GetInt64();
                            SetItemsCountString();
                            ViewModel.FilesAndFoldersCountVisibility = true;
                        }
                        else
                        {
                            ViewModel.FilesAndFoldersCountVisibility = false;
                        }
                        ViewModel.ItemCreatedTimestampVisibility = false;
                        ViewModel.ItemAccessedTimestampVisibility = false;
                        ViewModel.ItemModifiedTimestampVisibility = false;
                        ViewModel.LastSeparatorVisibility = false;
                    }
                }
            }
            else
            {
                GetFolderSize(folderPath, TokenSource.Token);
            }
        }

        private async void GetFolderSize(string folderPath, CancellationToken token)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                // In MTP devices calculating folder size would be too slow
                // Also should use StorageFolder methods instead of FindFirstFileExFromApp
                return;
            }

            ViewModel.ItemSizeVisibility = true;
            ViewModel.ItemSizeProgressVisibility = true;

            var fileSizeTask = Task.Run(async () =>
            {
                var size = await CalculateFolderSizeAsync(folderPath, token);
                return size;
            });
            try
            {
                var folderSize = await fileSizeTask;
                ViewModel.ItemSizeBytes = folderSize;
                ViewModel.ItemSize = folderSize.ToLongSizeString();
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
            }
            ViewModel.ItemSizeProgressVisibility = false;

            SetItemsCountString();
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsHidden":
                    if (ViewModel.IsHidden)
                    {
                        NativeFileOperationsHelper.SetFileAttribute(
                            Item.ItemPath, System.IO.FileAttributes.Hidden);
                    }
                    else
                    {
                        NativeFileOperationsHelper.UnsetFileAttribute(
                            Item.ItemPath, System.IO.FileAttributes.Hidden);
                    }
                    break;

                case "ShortcutItemPath":
                case "ShortcutItemWorkingDir":
                case "ShortcutItemArguments":
                    var tmpItem = (ShortcutItem)Item;
                    if (string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath))
                    {
                        return;
                    }

                    var connection = await AppServiceConnectionHelper.Instance;
                    if (connection != null)
                    {
                        var value = new ValueSet()
                        {
                            { "Arguments", "FileOperation" },
                            { "fileop", "UpdateLink" },
                            { "filepath", Item.ItemPath },
                            { "targetpath", ViewModel.ShortcutItemPath },
                            { "arguments", ViewModel.ShortcutItemArguments },
                            { "workingdir", ViewModel.ShortcutItemWorkingDir },
                            { "runasadmin", tmpItem.RunAsAdmin },
                        };
                        await connection.SendMessageAsync(value);
                    }
                    break;
            }
        }
    }
}