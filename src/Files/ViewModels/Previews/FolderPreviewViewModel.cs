using Files.Enums;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Services;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
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
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private BaseStorageFolder Folder { get; set; }

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

            Folder = await StorageFileExtensions.DangerousGetFolderFromPathAsync(Item.ItemPath);
            var items = await Folder.GetItemsAsync();

            var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(Folder, 400, ThumbnailMode.SingleItem);
            iconData ??= await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.ItemPath, 400);
            if (iconData != null)
            {
                Thumbnail = await iconData.ToBitmapAsync();
            }

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

            if (UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled)
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