using ByteSizeLib;
using Files.Filesystem;
using Files.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Files.View_Models.Properties
{
    class FolderProperties : BaseProperties
    {
        public FolderProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
        }

        public async override void GetProperties()
        {
            StorageFolder storageFolder;
            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                storageFolder = await StorageFolder.GetFolderFromPathAsync(ViewModel.Item.ItemPath);
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
                            ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(DateTime.FromBinary((long)response.Message["DateCreated"]));
                            ViewModel.ItemSizeBytes = (long)response.Message["BinSize"];
                            ViewModel.ItemSize = ByteSize.FromBytes((long)response.Message["BinSize"]).ToString();
                            ViewModel.FilesCount = (int)(long)response.Message["NumItems"];
                            SetItemsCountString();
                            ViewModel.ItemAccessedTimestamp = ListedItem.GetFriendlyDate(DateTime.FromBinary((long)response.Message["DateAccessed"]));
                            ViewModel.ItemFileOwnerVisibility = Visibility.Collapsed;
                            ViewModel.ItemSizeVisibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    storageFolder = await StorageFolder.GetFolderFromPathAsync(parentDirectory.ItemPath);
                    ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(storageFolder.DateCreated);
                    GetOtherProperties(storageFolder.Properties);
                    GetFolderSize(storageFolder, TokenSource.Token);
                }
            }
        }

        private async void GetFolderSize(StorageFolder storageFolder, CancellationToken token)
        {
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
    }
}
