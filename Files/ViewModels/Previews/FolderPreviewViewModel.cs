using Files.Enums;
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
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

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
                    Value = Extensions.DateTimeExtensions.GetFriendlyDateFromFormat(info.DateModified, returnformat, true)
                },
                new FileProperty()
                {
                    NameResource = "PropertyDateCreated",
                    Value = Extensions.DateTimeExtensions.GetFriendlyDateFromFormat(info.ItemDate, returnformat, true)
                },
                new FileProperty()
                {
                    NameResource = "PropertyItemPathDisplay",
                    Value = Folder.Path,
                }
            };

            if(App.AppSettings.AreFileTagsEnabled)
            {
                Item.FileDetails.Add(new FileProperty()
                {
                    NameResource = "DetailsViewHeaderFlyout_ShowFileTag/Text",
                    Value = Item.FileTagUI?.TagName
                });
            }
        }
    }
}