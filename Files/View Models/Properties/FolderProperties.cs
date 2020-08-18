using ByteSizeLib;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Files.View_Models.Properties
{
    internal class FolderProperties : BaseProperties
    {
        public ListedItem Item { get; }

        public FolderProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher, ListedItem item)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
            Item = item;

            GetBaseProperties();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        public override void GetBaseProperties()
        {
            if (Item != null)
            {
                ViewModel.ItemName = Item.ItemName;
                ViewModel.ItemType = Item.ItemType;
                ViewModel.ItemPath = Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath;
                ViewModel.ItemModifiedTimestamp = Item.ItemDateModified;
                //ViewModel.FileIconSource = Item.FileImage;
                ViewModel.LoadFolderGlyph = Item.LoadFolderGlyph;
                ViewModel.LoadUnknownTypeGlyph = Item.LoadUnknownTypeGlyph;
                ViewModel.LoadFileIcon = Item.LoadFileIcon;
                ViewModel.ContainsFilesOrFolders = Item.ContainsFilesOrFolders;

                if (Item.IsShortcutItem)
                {
                    var shortcutItem = (ShortcutItem)Item;
                    ViewModel.ShortcutItemType = ResourceController.GetTranslation("PropertiesShortcutTypeFolder");
                    ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
                    ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
                    ViewModel.ShortcutItemWorkingDirVisibility = Visibility.Collapsed;
                    ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
                    ViewModel.ShortcutItemArgumentsVisibility = Visibility.Collapsed;
                    ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
                    {
                        var folderUri = new Uri("files-uwp:" + "?folder=" + Path.GetDirectoryName(ViewModel.ShortcutItemPath));
                        await Windows.System.Launcher.LaunchUriAsync(folderUri);
                    }, () =>
                    {
                        return !string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath);
                    }, false);
                }
            }
        }

        public async override void GetSpecialProperties()
        {
            if (Item.IsShortcutItem)
            {
                ViewModel.ItemSizeVisibility = Visibility.Visible;
                ViewModel.ItemSize = ByteSize.FromBytes(Item.FileSizeBytes).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSize.FromBytes(Item.FileSizeBytes).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";
                ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
                ViewModel.ItemAccessedTimestamp = Item.ItemDateAccessed;
                // Can't show any other property
                return;
            }

            StorageFolder storageFolder;
            var isItemSelected = await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => App.CurrentInstance.ContentPage.IsItemSelected);
            if (isItemSelected)
            {
                storageFolder = await ItemViewModel.GetFolderFromPathAsync(Item.ItemPath);
                ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(storageFolder.DateCreated);
                GetOtherProperties(storageFolder.Properties);
                GetFolderSize(storageFolder, TokenSource.Token);
            }
            else
            {
                var parentDirectory = App.CurrentInstance.FilesystemViewModel.CurrentFolder;
                if (parentDirectory.ItemPath.StartsWith(App.AppSettings.RecycleBinPath))
                {
                    // GetFolderFromPathAsync cannot access recyclebin folder
                    if (App.Connection != null)
                    {
                        var value = new ValueSet();
                        value.Add("Arguments", "RecycleBin");
                        value.Add("action", "Query");
                        // Send request to fulltrust process to get recyclebin properties
                        var response = await App.Connection.SendMessageAsync(value);
                        if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
                        {
                            if (response.Message.TryGetValue("BinSize", out var binSize))
                            {
                                ViewModel.ItemSizeBytes = (long)binSize;
                                ViewModel.ItemSize = ByteSize.FromBytes((long)binSize).ToString();
                                ViewModel.ItemSizeVisibility = Visibility.Visible;
                            }
                            else
                            {
                                ViewModel.ItemSizeVisibility = Visibility.Collapsed;
                            }
                            if (response.Message.TryGetValue("NumItems", out var numItems))
                            {
                                ViewModel.FilesCount = (int)(long)numItems;
                                SetItemsCountString();
                                ViewModel.FilesAndFoldersCountVisibility = Visibility.Visible;
                            }
                            else
                            {
                                ViewModel.FilesAndFoldersCountVisibility = Visibility.Collapsed;
                            }
                            ViewModel.ItemCreatedTimestampVisibiity = Visibility.Collapsed;
                            ViewModel.ItemAccessedTimestampVisibility = Visibility.Collapsed;
                            ViewModel.ItemModifiedTimestampVisibility = Visibility.Collapsed;
                            ViewModel.ItemFileOwnerVisibility = Visibility.Collapsed;
                            ViewModel.LastSeparatorVisibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    storageFolder = await ItemViewModel.GetFolderFromPathAsync(parentDirectory.ItemPath);
                    ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(storageFolder.DateCreated);
                    GetOtherProperties(storageFolder.Properties);
                    GetFolderSize(storageFolder, TokenSource.Token);
                }
            }
        }

        private async void GetFolderSize(StorageFolder storageFolder, CancellationToken token)
        {
            if (string.IsNullOrEmpty(storageFolder.Path))
            {
                // In MTP devices calculating folder size would be too slow
                // Also should use StorageFolder methods instead of FindFirstFileExFromApp
                return;
            }

            ViewModel.ItemSizeVisibility = Visibility.Visible;
            ViewModel.ItemSizeProgressVisibility = Visibility.Visible;

            var fileSizeTask = Task.Run(async () =>
            {
                var size = await CalculateFolderSizeAsync(storageFolder.Path, token);
                return size;
            });
            try
            {
                var folderSize = await fileSizeTask;
                ViewModel.ItemSizeBytes = folderSize;
                ViewModel.ItemSize = ByteSize.FromBytes(folderSize).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSize.FromBytes(folderSize).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
            }
            ViewModel.ItemSizeProgressVisibility = Visibility.Collapsed;

            SetItemsCountString();
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ShortcutItemPath":
                case "ShortcutItemWorkingDir":
                case "ShortcutItemArguments":
                    var tmpItem = (ShortcutItem)Item;
                    if (string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath))
                        return;
                    if (App.Connection != null)
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
                        await App.Connection.SendMessageAsync(value);
                    }
                    break;
            }
        }
    }
}