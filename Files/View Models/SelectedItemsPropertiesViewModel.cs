using Files.DataModels;
using Files.Filesystem;
using GalaSoft.MvvmLight;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.View_Models
{
    public class SelectedItemsPropertiesViewModel : ViewModelBase
    {
        #region Properties
        private StorageItemProperties _properties;

        public StorageItemProperties Properties
        {
            get => _properties;
            set => Set(ref _properties, value);
        }

        public ProgressBar ItemMD5HashProgress
        {
            get;set;
        }

        #endregion
        #region Constructors
        public SelectedItemsPropertiesViewModel()
        {
            Properties = new StorageItemProperties();
        }
        #endregion
        #region Methods
        public async void GetOtherPropeties(StorageItemContentProperties properties)
        {
            string dateAccessedProperty = "System.DateAccessed";
            string fileOwnerProperty = "System.FileOwner";
            List<string> propertiesName = new List<string>();
            propertiesName.Add(dateAccessedProperty);
            propertiesName.Add(fileOwnerProperty);
            IDictionary<string, object> extraProperties = await properties.RetrievePropertiesAsync(propertiesName);
            Properties.ItemAccessedTimestamp = ListedItem.GetFriendlyDate((DateTimeOffset)extraProperties[dateAccessedProperty]);
            Properties.ItemFileOwner = extraProperties[fileOwnerProperty].ToString();
        }
        private async void GetFolderSize(StorageFolder storageFolder)
        {
            var folders = storageFolder.CreateFileQuery(CommonFileQuery.OrderByName);
            var fileSizeTasks = (await folders.GetFilesAsync()).Select(async file => (await file.GetBasicPropertiesAsync()).Size);
            var sizes = await Task.WhenAll(fileSizeTasks);
            var folderSize = sizes.Sum(singleSize => (long)singleSize);
            Properties.ItemsSize = ByteSizeLib.ByteSize.FromBytes(folderSize).ToString();
        }
        public async Task GetPropertiesAsync(CancellationTokenSource _tokenSource)
        {
            Properties = new StorageItemProperties();
            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                var selectedItem = App.CurrentInstance.ContentPage.SelectedItem;
                IStorageItem selectedStorageItem = null;

                if (selectedItem.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    var file = await StorageFile.GetFileFromPathAsync(selectedItem.ItemPath);
                    selectedStorageItem = file;
                    GetOtherPropeties(file.Properties);
                    Properties.ItemsSize = selectedItem.FileSize;
                }
                else if (selectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    var storageFolder = await StorageFolder.GetFolderFromPathAsync(selectedItem.ItemPath);
                    selectedStorageItem = storageFolder;
                    GetOtherPropeties(storageFolder.Properties);
                    GetFolderSize(storageFolder);
                }
                Properties.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(selectedStorageItem.DateCreated);
                if (selectedItem.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    // Get file MD5 hash
                    var hashAlgTypeName = HashAlgorithmNames.Md5;
                    Properties.ItemMD5HashProgressVisibility = Visibility.Visible;
                    Properties.ItemMD5Hash = await App.CurrentInstance.InteractionOperations.GetHashForFile(selectedItem, hashAlgTypeName, _tokenSource.Token, ItemMD5HashProgress);
                    Properties.ItemMD5HashProgressVisibility = Visibility.Collapsed;
                    Properties.ItemMD5HashVisibility = Visibility.Visible;
                }
                else if (selectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    Properties.ItemMD5HashVisibility = Visibility.Collapsed;
                    Properties.ItemMD5HashProgressVisibility = Visibility.Collapsed;
                }
            }
            else
            {
                var parentDirectory = App.CurrentInstance.ViewModel.CurrentFolder;
                if (parentDirectory.ItemPath.StartsWith(App.AppSettings.RecycleBinPath))
                {
                    // GetFolderFromPathAsync cannot access recyclebin folder
                    // Currently a fake timestamp is used                
                }
                else
                {
                    var parentDirectoryStorageItem = await StorageFolder.GetFolderFromPathAsync(parentDirectory.ItemPath);
                    Properties.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(parentDirectoryStorageItem.DateCreated);
                }

                Properties.ItemMD5HashVisibility = Visibility.Collapsed;
                Properties.ItemMD5HashProgressVisibility = Visibility.Collapsed;
            }
        }
        #endregion
    }    
}