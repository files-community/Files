using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using GalaSoft.MvvmLight;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files
{
    public sealed partial class Properties : Page
    {
        private static AppWindowTitleBar _TitleBar;
        private CancellationTokenSource _tokenSource;

        public AppWindow propWindow;

        public ItemPropertiesViewModel ItemProperties { get; } = new ItemPropertiesViewModel();

        public Properties()
        {
            this.InitializeComponent();
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                Loaded += Properties_Loaded;
            }
            else
            {
                this.OKButton.Visibility = Visibility.Collapsed;
            }
            App.AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
        }

        private async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            _tokenSource?.Dispose();
            _tokenSource = new CancellationTokenSource();
            Unloaded += Properties_Unloaded;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                // Collect AppWindow-specific info
                propWindow = Interaction.AppWindows[UIContext];
                // Set properties window titleBar style
                _TitleBar = propWindow.TitleBar;
                _TitleBar.ButtonBackgroundColor = Colors.Transparent;
                _TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                App.AppSettings.UpdateThemeElements.Execute(null);
            }

            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                var selectedItem = App.CurrentInstance.ContentPage.SelectedItem;
                IStorageItem selectedStorageItem = null;

                ItemProperties.ItemSizeProgressVisibility = Visibility.Visible;
                if (selectedItem.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    selectedStorageItem = await StorageFile.GetFileFromPathAsync(selectedItem.ItemPath);
                    ItemProperties.ItemSize = selectedItem.FileSize;
                    ItemProperties.ItemSizeProgressVisibility = Visibility.Collapsed;
                }
                else if (selectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    var storageFolder = await StorageFolder.GetFolderFromPathAsync(selectedItem.ItemPath);
                    selectedStorageItem = storageFolder;
                    GetFolderSize(storageFolder, _tokenSource.Token);
                }

                ItemProperties.ItemName = selectedItem.ItemName;
                ItemProperties.ItemType = selectedItem.ItemType;
                ItemProperties.ItemPath = selectedItem.ItemPath;

                ItemProperties.LoadFileIcon = selectedItem.LoadFileIcon;
                ItemProperties.LoadFolderGlyph = selectedItem.LoadFolderGlyph;
                ItemProperties.LoadUnknownTypeGlyph = selectedItem.LoadUnknownTypeGlyph;
                ItemProperties.ItemModifiedTimestamp = selectedItem.ItemDateModified;
                ItemProperties.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(selectedStorageItem.DateCreated);

                if (!App.CurrentInstance.ContentPage.SelectedItem.LoadFolderGlyph)
                {
                    var thumbnail = await (await StorageFile.GetFileFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.ItemPath)).GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 80, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(thumbnail);
                    ItemProperties.FileIconSource = bitmap;
                }

                if (selectedItem.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    // Get file MD5 hash
                    var hashAlgTypeName = HashAlgorithmNames.Md5;
                    ItemProperties.ItemMD5HashProgressVisibility = Visibility.Visible;
                    ItemProperties.ItemMD5Hash = await App.CurrentInstance.InteractionOperations.GetHashForFile(selectedItem, hashAlgTypeName, _tokenSource.Token, ItemMD5HashProgress);
                    ItemProperties.ItemMD5HashProgressVisibility = Visibility.Collapsed;
                    ItemProperties.ItemMD5HashVisibility = Visibility.Visible;
                }
                else if (selectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    ItemProperties.ItemMD5HashVisibility = Visibility.Collapsed;
                    ItemProperties.ItemMD5HashProgressVisibility = Visibility.Collapsed;
                }
            }
            else
            {
                var parentDirectory = App.CurrentInstance.ViewModel.CurrentFolder;
                if (parentDirectory.ItemPath.StartsWith(App.AppSettings.RecycleBinPath))
                {
                    // GetFolderFromPathAsync cannot access recyclebin folder
                    // Currently a fake timestamp is used
                    ItemProperties.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(parentDirectory.ItemDateModifiedReal);
                    ItemProperties.ItemSize = parentDirectory.FileSize;
                }
                else
                {
                    var parentDirectoryStorageItem = await StorageFolder.GetFolderFromPathAsync(parentDirectory.ItemPath);
                    ItemProperties.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(parentDirectoryStorageItem.DateCreated);
                }
                ItemProperties.ItemName = parentDirectory.ItemName;
                ItemProperties.ItemType = parentDirectory.ItemType;
                ItemProperties.ItemPath = parentDirectory.ItemPath;
                ItemProperties.LoadFileIcon = false;
                ItemProperties.LoadFolderGlyph = true;
                ItemProperties.LoadUnknownTypeGlyph = false;
                ItemProperties.ItemModifiedTimestamp = parentDirectory.ItemDateModified;
                ItemProperties.ItemMD5HashVisibility = Visibility.Collapsed;
                ItemProperties.ItemMD5HashProgressVisibility = Visibility.Collapsed;
            }
        }

        private void Properties_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_tokenSource != null && !_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _tokenSource = null;
            }
            Unloaded -= Properties_Unloaded;
        }

        private async void GetFolderSize(StorageFolder storageFolder, CancellationToken token)
        {
            ItemProperties.ItemSize = ByteSizeLib.ByteSize.FromBytes(0).ToString();
            var folders = storageFolder.CreateFileQuery(CommonFileQuery.OrderByName);
            var fileSizeTask = Task.Run(async () =>
            {
                var count = 0;
                long size = 0;
                uint index = 0;
                do
                {
                    var files = await folders.GetFilesAsync(index, 16);
                    count = files.Count;
                    index += (uint)count;
                    size += (await Task.WhenAll(files.Select(async file => (await file.GetBasicPropertiesAsync()).Size)))
                        .Sum(size => (long)size);
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        ItemProperties.ItemSize = ByteSizeLib.ByteSize.FromBytes(size).ToString();
                    });
                } while (count >= 16 && !token.IsCancellationRequested);
                return size;
            });
            try
            {
                var folderSize = await fileSizeTask;
                ItemProperties.ItemSize = ByteSizeLib.ByteSize.FromBytes(folderSize).ToString();
                ItemProperties.ItemSizeProgressVisibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                ItemProperties.SizeCalcError = true;
            }
        }
        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                switch (ThemeHelper.RootTheme)
                {
                    case ElementTheme.Default:
                        _TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                        _TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                        break;

                    case ElementTheme.Light:
                        _TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                        _TitleBar.ButtonForegroundColor = Colors.Black;
                        break;

                    case ElementTheme.Dark:
                        _TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                        _TitleBar.ButtonForegroundColor = Colors.White;
                        break;
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            App.AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await propWindow.CloseAsync();
            }
            else
            {
                App.PropertiesDialogDisplay.Hide();
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _tokenSource = new CancellationTokenSource();
            }
        }
    }

    public class ItemPropertiesViewModel : ViewModelBase
    {
        private string _ItemName;
        private string _ItemType;
        private string _ItemPath;
        private string _ItemMD5Hash;
        private Visibility _ItemMD5HashVisibility;
        private Visibility _ItemMD5HashProgressVisibility;
        private string _ItemSize;
        private Visibility _ItemSizeProgressVisibility;
        private string _ItemCreatedTimestamp;
        private string _ItemModifiedTimestamp;
        private ImageSource _FileIconSource;
        private bool _LoadFolderGlyph;
        private bool _LoadUnknownTypeGlyph;
        private bool _LoadFileIcon;
        private bool _SizeCalcError;

        public string ItemName
        {
            get => _ItemName;
            set => Set(ref _ItemName, value);
        }

        public string ItemMD5Hash
        {
            get => _ItemMD5Hash;
            set => Set(ref _ItemMD5Hash, value);
        }

        public Visibility ItemMD5HashVisibility
        {
            get => _ItemMD5HashVisibility;
            set => Set(ref _ItemMD5HashVisibility, value);
        }

        public Visibility ItemMD5HashProgressVisibility
        {
            get => _ItemMD5HashProgressVisibility;
            set => Set(ref _ItemMD5HashProgressVisibility, value);
        }

        public string ItemType
        {
            get => _ItemType;
            set => Set(ref _ItemType, value);
        }

        public string ItemPath
        {
            get => _ItemPath;
            set => Set(ref _ItemPath, value);
        }

        public string ItemSize
        {
            get => _ItemSize;
            set => Set(ref _ItemSize, value);
        }

        public Visibility ItemSizeProgressVisibility
        {
            get => _ItemSizeProgressVisibility;
            set => Set(ref _ItemSizeProgressVisibility, value);
        }

        public bool SizeCalcError
        {
            get => _SizeCalcError;
            set => Set(ref _SizeCalcError, value);
        }

        public string ItemCreatedTimestamp
        {
            get => _ItemCreatedTimestamp;
            set => Set(ref _ItemCreatedTimestamp, value);
        }

        public string ItemModifiedTimestamp
        {
            get => _ItemModifiedTimestamp;
            set => Set(ref _ItemModifiedTimestamp, value);
        }

        public ImageSource FileIconSource
        {
            get => _FileIconSource;
            set => Set(ref _FileIconSource, value);
        }

        public bool LoadFolderGlyph
        {
            get => _LoadFolderGlyph;
            set => Set(ref _LoadFolderGlyph, value);
        }

        public bool LoadUnknownTypeGlyph
        {
            get => _LoadUnknownTypeGlyph;
            set => Set(ref _LoadUnknownTypeGlyph, value);
        }

        public bool LoadFileIcon
        {
            get => _LoadFileIcon;
            set => Set(ref _LoadFileIcon, value);
        }
    }
}