using ByteSizeLib;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Files.ViewModels.Properties
{
    internal class FolderProperties : BaseProperties
    {
        public ListedItem Item { get; }

        public FolderProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher, ListedItem item, IShellPage instance)
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
                ViewModel.ItemName = Item.ItemName;
                ViewModel.OriginalItemName = Item.ItemName;
                ViewModel.ItemType = Item.ItemType;
                ViewModel.ItemPath = (Item as RecycleBinItem)?.ItemOriginalFolder ??
                    (Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath);
                ViewModel.ItemModifiedTimestamp = Item.ItemDateModified;
                ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
                ViewModel.LoadCustomIcon = Item.LoadCustomIcon;
                ViewModel.CustomIconSource = Item.CustomIconSource;
                ViewModel.LoadFileIcon = Item.LoadFileIcon;
                ViewModel.ContainsFilesOrFolders = Item.ContainsFilesOrFolders;

                if (Item.IsShortcutItem)
                {
                    var shortcutItem = (ShortcutItem)Item;
                    ViewModel.ShortcutItemType = "Folder".GetLocalized();
                    ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
                    ViewModel.IsShortcutItemPathReadOnly = false;
                    ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
                    ViewModel.ShortcutItemWorkingDirVisibility = false;
                    ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
                    ViewModel.ShortcutItemArgumentsVisibility = false;
                    ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
                    {
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(
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

            var fileIconData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.ItemPath, 80, Windows.Storage.FileProperties.ThumbnailMode.SingleItem);
            if (fileIconData != null)
            {
                ViewModel.IconData = fileIconData;
                ViewModel.LoadFolderGlyph = false;
                ViewModel.LoadFileIcon = true;
            }

            if (Item.IsShortcutItem)
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

            BaseStorageFolder storageFolder;
            try
            {
                storageFolder = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync((Item as ShortcutItem)?.TargetPath ?? Item.ItemPath);
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
                // Could not access folder, can't show any other property
                return;
            }

            if (storageFolder != null)
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
                ViewModel.ItemCreatedTimestamp = storageFolder.DateCreated.GetFriendlyDateFromFormat(returnformat);
                if (storageFolder.Properties != null)
                {
                    GetOtherProperties(storageFolder.Properties);
                }
                GetFolderSize(storageFolder, TokenSource.Token);
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
                            ViewModel.ItemSizeBytes = (long)binSize;
                            ViewModel.ItemSize = ByteSize.FromBytes((long)binSize).ToString();
                            ViewModel.ItemSizeVisibility = true;
                        }
                        else
                        {
                            ViewModel.ItemSizeVisibility = false;
                        }
                        if (response.TryGetValue("NumItems", out var numItems))
                        {
                            ViewModel.FilesCount = (int)(long)numItems;
                            SetItemsCountString();
                            ViewModel.FilesAndFoldersCountVisibility = true;
                        }
                        else
                        {
                            ViewModel.FilesAndFoldersCountVisibility = false;
                        }
                        ViewModel.ItemCreatedTimestampVisibiity = false;
                        ViewModel.ItemAccessedTimestampVisibility = false;
                        ViewModel.ItemModifiedTimestampVisibility = false;
                        ViewModel.LastSeparatorVisibility = false;
                    }
                }
            }
        }

        private async void GetFolderSize(BaseStorageFolder storageFolder, CancellationToken token)
        {
            if (string.IsNullOrEmpty(storageFolder.Path))
            {
                // In MTP devices calculating folder size would be too slow
                // Also should use StorageFolder methods instead of FindFirstFileExFromApp
                return;
            }

            ViewModel.ItemSizeVisibility = true;
            ViewModel.ItemSizeProgressVisibility = true;

            var fileSizeTask = Task.Run(async () =>
            {
                var size = await CalculateFolderSizeAsync(storageFolder.Path, token);
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