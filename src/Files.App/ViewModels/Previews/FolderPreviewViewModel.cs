using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Shared.Services.DateTimeFormatter;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.ViewModels.Previews
{
    public class FolderPreviewViewModel
    {
        private readonly IPreferencesSettingsService preferencesSettingsService = Ioc.Default.GetService<IPreferencesSettingsService>();
        private static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetService<IDateTimeFormatter>();

        public ListedItem Item { get; }

        public BitmapImage Thumbnail { get; set; } = new BitmapImage();

        private BaseStorageFolder Folder { get; set; }

        public FolderPreviewViewModel(ListedItem item) => Item = item;

        public async Task LoadAsync() => await LoadPreviewAndDetailsAsync();

        private async Task LoadPreviewAndDetailsAsync()
        {
            var rootItem = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(Item.ItemPath));
            Folder = await StorageFileExtensions.DangerousGetFolderFromPathAsync(Item.ItemPath, rootItem);
            var items = await Folder.GetItemsAsync();

            var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(Folder, 400, ThumbnailMode.SingleItem);
            iconData ??= await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.ItemPath, 400, true);
            if (iconData is not null)
            {
                Thumbnail = await iconData.ToBitmapAsync();
            }

            var info = await Folder.GetBasicPropertiesAsync();
            Item.FileDetails = new()
            {
                GetFileProperty("PropertyItemCount", items.Count),
                GetFileProperty("PropertyDateModified", dateTimeFormatter.ToLongLabel(info.DateModified)),
                GetFileProperty("PropertyDateCreated", dateTimeFormatter.ToLongLabel(info.ItemDate)),
                GetFileProperty("PropertyItemPathDisplay", Folder.Path),
            };

            Item.FileDetails.Add(GetFileProperty("FileTags",
                Item.FileTagsUI is not null ? string.Join(',', Item.FileTagsUI.Select(x => x.TagName)) : null));
        }

        private static FileProperty GetFileProperty(string nameResource, object value)
            => new() { NameResource = nameResource, Value = value };
    }
}