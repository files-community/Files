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
        #region ItemProperties
        private string _ItemName;
        public string ItemName
        {
            get => _ItemName;
            set => Set(ref _ItemName, value);
        }
        private string _ItemType;
        public string ItemType
        {
            get => _ItemType;
            set => Set(ref _ItemType, value);
        }
        private string _ItemPath;
        public string ItemPath
        {
            get => _ItemPath;
            set => Set(ref _ItemPath, value);
        }
        private string _ItemModifiedTimestamp;
        public string ItemModifiedTimestamp
        {
            get => _ItemModifiedTimestamp;
            set => Set(ref _ItemModifiedTimestamp, value);
        }
        private ImageSource _FileIconSource;
        public ImageSource FileIconSource
        {
            get => _FileIconSource;
            set => Set(ref _FileIconSource, value);
        }
        private bool _LoadFolderGlyph;
        public bool LoadFolderGlyph
        {
            get => _LoadFolderGlyph;
            set => Set(ref _LoadFolderGlyph, value);
        }
        private bool _LoadUnknownTypeGlyph;
        public bool LoadUnknownTypeGlyph
        {
            get => _LoadUnknownTypeGlyph;
            set => Set(ref _LoadUnknownTypeGlyph, value);
        }
        private bool _LoadFileIcon;
        public bool LoadFileIcon
        {
            get => _LoadFileIcon;
            set => Set(ref _LoadFileIcon, value);
        }
      
        private string _ItemsSize;

        public string ItemsSize
        {
            get => _ItemsSize;
            set => Set(ref _ItemsSize, value);
        }

        private string _SelectedItemsCount;

        public string SelectedItemsCount
        {
            get => _SelectedItemsCount;
            set => Set(ref _SelectedItemsCount, value);
        }

        private bool _IsItemSelected;

        public bool IsItemSelected
        {
            get => _IsItemSelected;
            set => Set(ref _IsItemSelected, value);
        }
        private string _ItemCreatedTimestamp;

        public string ItemCreatedTimestamp
        {
            get => _ItemCreatedTimestamp;
            set => Set(ref _ItemCreatedTimestamp, value);
        }
        public string _ItemAccessedTimestamp;
        public string ItemAccessedTimestamp
        {
            get => _ItemAccessedTimestamp;
            set => Set(ref _ItemAccessedTimestamp, value);
        }
        public string _ItemFileOwner;
        public string ItemFileOwner
        {
            get => _ItemFileOwner;
            set => Set(ref _ItemFileOwner, value);
        }
        public string _ItemMD5Hash;
        public string ItemMD5Hash
        {
            get => _ItemMD5Hash;
            set => Set(ref _ItemMD5Hash, value);
        }
        public Visibility _ItemMD5HashVisibility;

        public Visibility ItemMD5HashVisibility
        {
            get => _ItemMD5HashVisibility;
            set => Set(ref _ItemMD5HashVisibility, value);
        }
        public Visibility _ItemMD5HashProgressVisibiity;

        public Visibility ItemMD5HashProgressVisibility
        {
            get => _ItemMD5HashProgressVisibiity;
            set => Set(ref _ItemMD5HashProgressVisibiity, value);
        }
        #endregion
        #region Properties
        public ProgressBar ItemMD5HashProgress
        {
            get; set;
        }
        public ListedItem Item { get; }
        #endregion
        #region Constructors
        public SelectedItemsPropertiesViewModel(ListedItem item)
        {
            Item = item;

            ItemName = Item?.ItemName;
            ItemType = Item?.ItemType;
            ItemPath = Item?.ItemPath;
            ItemModifiedTimestamp = Item?.ItemDateModified;
            FileIconSource = Item?.FileImage;
            LoadFolderGlyph = Item != null ? Item.LoadFolderGlyph : false;
            LoadUnknownTypeGlyph = Item != null ? Item.LoadUnknownTypeGlyph : false;
            LoadFileIcon = Item != null ? Item.LoadFileIcon : false;      
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
            ItemAccessedTimestamp = ListedItem.GetFriendlyDate((DateTimeOffset)extraProperties[dateAccessedProperty]);
            ItemFileOwner = extraProperties[fileOwnerProperty].ToString();
        }
        private async void GetFolderSize(StorageFolder storageFolder)
        {
            var folders = storageFolder.CreateFileQuery(CommonFileQuery.OrderByName);
            var fileSizeTasks = (await folders.GetFilesAsync()).Select(async file => (await file.GetBasicPropertiesAsync()).Size);
            var sizes = await Task.WhenAll(fileSizeTasks);
            var folderSize = sizes.Sum(singleSize => (long)singleSize);
            ItemsSize = ByteSizeLib.ByteSize.FromBytes(folderSize).ToString();
        }
        public async Task GetPropertiesAsync(CancellationTokenSource _tokenSource)
        {
            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                IStorageItem selectedStorageItem = null;

                if (Item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    var file = await StorageFile.GetFileFromPathAsync(Item.ItemPath);
                    selectedStorageItem = file;
                    GetOtherPropeties(file.Properties);
                    ItemsSize = Item.FileSize;
                }
                else if (Item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    var storageFolder = await StorageFolder.GetFolderFromPathAsync(Item.ItemPath);
                    selectedStorageItem = storageFolder;
                    GetOtherPropeties(storageFolder.Properties);
                    GetFolderSize(storageFolder);
                }
                ItemCreatedTimestamp = ListedItem.GetFriendlyDate(selectedStorageItem.DateCreated);
                if (Item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    // Get file MD5 hash
                    var hashAlgTypeName = HashAlgorithmNames.Md5;
                    ItemMD5HashProgressVisibility = Visibility.Visible;
                    ItemMD5Hash = await App.CurrentInstance.InteractionOperations.GetHashForFile(Item, hashAlgTypeName, _tokenSource.Token, ItemMD5HashProgress);
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
                    ItemMD5HashVisibility = Visibility.Visible;
                }
                else if (Item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    ItemMD5HashVisibility = Visibility.Collapsed;
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
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
                    ItemCreatedTimestamp = ListedItem.GetFriendlyDate(parentDirectoryStorageItem.DateCreated);
                }

                ItemMD5HashVisibility = Visibility.Collapsed;
                ItemMD5HashProgressVisibility = Visibility.Collapsed;
            }
        }
        #endregion
    }    
}