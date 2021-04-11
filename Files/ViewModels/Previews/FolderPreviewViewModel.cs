using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Previews
{
    public class FolderPreviewViewModel
    {
        private StorageFolder Folder { get; set; }

        public ListedItem Item { get; set; }

        public BitmapImage Thumbnail { get; set; } = new BitmapImage();

        public FolderPreviewViewModel(ListedItem item)
        {
            Item = item;
        }

        public async Task LoadAsync()
        {
            await LoadPreviewAndDetailsAsync();
        }

        private async Task LoadPreviewAndDetailsAsync()
        {
            Folder = await StorageFolder.GetFolderFromPathAsync(Item.ItemPath);
            var items = await Folder.GetItemsAsync();

            using var icon = await Folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 400);
            await Thumbnail.SetSourceAsync(icon);

            var info = await Folder.GetBasicPropertiesAsync();
            Item.FileDetails = new ObservableCollection<FileProperty>()
            {
                new FileProperty()
                {
                    NameResource = "PropertyItemCount",
                    Value = items.Count,
                },
                new FileProperty()
                {
                    NameResource = "PropertyDateModified",
                    Value = info.DateModified,
                },
                new FileProperty()
                {
                    NameResource = "PropertyDateCreated",
                    Value = info.ItemDate,
                },
                new FileProperty()
                {
                    NameResource = "PropertyItemName",
                    Value = Folder.Name,
                },
                new FileProperty()
                {
                    NameResource = "PropertyItemPathDisplay",
                    Value = Folder.Path,
                }
            };
        }
    }
}