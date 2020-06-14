using Files.Filesystem;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task GetPropertiesAsync()
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
                    Properties.ItemMD5Hash = await App.CurrentInstance.InteractionOperations.GetHashForFile(selectedItem, hashAlgTypeName);
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
    public class StorageItemProperties : ObservableObject
    {
        #region readonly properties
        public string ItemName
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemName;
                }
                else
                {
                    return null;
                }
            }
        }

        public string ItemType
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemType;
                }
                else
                {
                    return null;
                }
            }
        }

        public string ItemPath
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemPath;
                }
                else
                {
                    return null;
                }
            }
        }

        public string ItemModifiedTimestamp
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemDateModified;
                }
                else
                {
                    return null;
                }
            }
        }

        public ImageSource FileIconSource
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.FileImage;
                }
                else
                {
                    return null;
                }
            }
        }
        public bool LoadFolderGlyph
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.LoadFolderGlyph;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool LoadUnknownTypeGlyph
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.LoadUnknownTypeGlyph;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool LoadFileIcon
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.LoadFileIcon;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion
        #region Properties
        private String _ItemsSize;

        public String ItemsSize
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

    }
}