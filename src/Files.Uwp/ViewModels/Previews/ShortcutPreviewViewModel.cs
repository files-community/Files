using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Uwp.ViewModels.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Uwp.ViewModels.Previews
{
    internal class ShortcutPreviewViewModel : BasePreviewModel
    {
        public ShortcutPreviewViewModel(ListedItem item) : base(item) {}

        public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
        {
            var item = Item as ShortcutItem;
            var details = new List<FileProperty>
            {
                GetFileProperty("PropertyItemPathDisplay", item.ItemPath),
                GetFileProperty("PropertyItemName", item.ItemName),
                GetFileProperty("PropertyItemTypeText", item.ItemType),
                GetFileProperty("PropertyItemTarget", item.TargetPath),
                GetFileProperty("Arguments", item.Arguments),
            };
            await LoadItemThumbnail();
            return details;
        }

        public override async Task LoadAsync()
        {
            var details = await LoadPreviewAndDetailsAsync();
            Item.FileDetails?.Clear();
            Item.FileDetails = new(details.OfType<FileProperty>());
        }

        private async Task LoadItemThumbnail()
        {
            var iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.ItemPath, 400);
            if (iconData is not null)
            {
                FileImage = await iconData.ToBitmapAsync();
            }
        }
    }
}