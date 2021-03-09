using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Previews
{
    public class FolderPreviewViewModel
    {
        private StorageFolder Folder { get; set; }

        public ListedItem Item { get; set; }

        public ObservableCollection<FolderContent> Contents { get; set; } = new ObservableCollection<FolderContent>();

        public FolderPreviewViewModel(ListedItem item)
        {
            Item = item;
            LoadPreviewAndDetailsAsync();
        }

        private async void LoadPreviewAndDetailsAsync()
        {
            Folder = await StorageFolder.GetFolderFromPathAsync(Item.ItemPath);
            var items = await Folder.GetItemsAsync();

            foreach (var item in items.Take(Constants.PreviewPane.FolderPreviewThumbnailCount))
            {
                if (item is StorageFile)
                {
                    var icon = await (item as StorageFile).GetThumbnailAsync(ThumbnailMode.SingleItem, 80, ThumbnailOptions.UseCurrentScale);
                    var imageSource = new BitmapImage();
                    if (icon != null)
                    {
                        await imageSource.SetSourceAsync(icon);
                    }

                    Contents.Add(new FolderContent
                    {
                        Image = imageSource
                    });
                }
            }

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

    public struct FolderContent
    {
        public BitmapImage Image;
    }
}