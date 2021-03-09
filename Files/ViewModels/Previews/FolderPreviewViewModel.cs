using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
            LoadPreviewAndDetailsAsync();
        }

        private async void LoadPreviewAndDetailsAsync()
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
                    LocalizedName = "Item count",
                    Value = items.Count,
                },
                new FileProperty()
                {
                    LocalizedName = "Date Modified",
                    Value = info.DateModified,
                },
                new FileProperty()
                {
                    LocalizedName = "Date Created",
                    Value = info.ItemDate,
                }
            };
        }
    }
}