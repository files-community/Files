using Files.Filesystem;
using Files.Interacts;
using GalaSoft.MvvmLight;
using System;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files
{

    public sealed partial class Properties : Page
    {
        public AppWindow propWindow;
        public ItemPropertiesViewModel itemProperties { get; } = new ItemPropertiesViewModel();
        public Properties()
        {
            this.InitializeComponent();
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                Loaded += Properties_Loaded;
            }
            else
            {
                this.OKButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private async void Properties_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Collect AppWindow-specific info
            propWindow = Interaction.AppWindows[this.UIContext];
            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                var selectedItem = App.CurrentInstance.ContentPage.SelectedItem;
                IStorageItem selectedStorageItem;
                try 
                {
                    selectedStorageItem = await StorageFolder.GetFolderFromPathAsync(selectedItem.FilePath);
                }
                catch (Exception)
                {
                    // Not a folder, so attempt to get as StorageFile
                    selectedStorageItem = await StorageFile.GetFileFromPathAsync(selectedItem.FilePath);
                }
                itemProperties.ItemName = selectedItem.FileName;
                itemProperties.ItemType = selectedItem.FileType;
                itemProperties.ItemPath = selectedItem.FilePath;
                itemProperties.ItemSize = selectedItem.FileSize;
                itemProperties.LoadFileIcon = selectedItem.FileIconVis == Windows.UI.Xaml.Visibility.Visible ? true : false;
                itemProperties.LoadFolderGlyph = selectedItem.FolderImg == Windows.UI.Xaml.Visibility.Visible ? true : false;
                itemProperties.LoadUnknownTypeGlyph = selectedItem.EmptyImgVis == Windows.UI.Xaml.Visibility.Visible ? true : false;
                itemProperties.ItemModifiedTimestamp = selectedItem.FileDate;
                itemProperties.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(selectedStorageItem.DateCreated);

                if (App.CurrentInstance.ContentPage.SelectedItem.FolderImg != Windows.UI.Xaml.Visibility.Visible)
                {
                    var thumbnail = await (await StorageFile.GetFileFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.FilePath)).GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 40, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(thumbnail);
                    itemProperties.FileIconSource = bitmap;
                }
            }
            else
            {
                var parentDirectory = App.CurrentInstance.ViewModel.currentFolder;
                var parentDirectoryStorageItem = await StorageFolder.GetFolderFromPathAsync(parentDirectory.FilePath);
                itemProperties.ItemName = parentDirectory.FileName;
                itemProperties.ItemType = parentDirectory.FileType;
                itemProperties.ItemPath = parentDirectory.FilePath;
                itemProperties.ItemSize = parentDirectory.FileSize;
                itemProperties.LoadFileIcon = false;
                itemProperties.LoadFolderGlyph = true;
                itemProperties.LoadUnknownTypeGlyph = false;
                itemProperties.ItemModifiedTimestamp = parentDirectory.FileDate;
                itemProperties.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(parentDirectoryStorageItem.DateCreated);
            }
        }

        private async void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await propWindow.CloseAsync();
            }
        }
    }

    public class ItemPropertiesViewModel : ViewModelBase
    {
        private string _ItemName;
        private string _ItemType;
        private string _ItemPath;
        private string _ItemSize;
        private string _ItemCreatedTimestamp;
        private string _ItemModifiedTimestamp;
        private ImageSource _FileIconSource;
        private bool _LoadFolderGlyph;
        private bool _LoadUnknownTypeGlyph;
        private bool _LoadFileIcon;

        public string ItemName
        {
            get => _ItemName;
            set => Set(ref _ItemName, value);
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
