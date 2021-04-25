using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.ViewModels.Previews
{
    internal class ShortcutPreviewViewModel : BasePreviewModel
    {
        public ShortcutPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public async override Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var item = Item as ShortcutItem;
            var details = new List<FileProperty>
            {
                new FileProperty()
                {
                    NameResource = "PropertyItemPathDisplay",
                    Value = item.ItemPath,
                },
                new FileProperty()
                {
                    NameResource = "PropertyItemName",
                    Value = item.ItemName,
                },
                new FileProperty()
                {
                    NameResource = "PropertyItemTypeText",
                    Value = item.ItemType,
                },
                new FileProperty()
                {
                    NameResource = "PropertyItemTarget",
                    Value = item.TargetPath,
                },
                new FileProperty()
                {
                    NameResource = "PropertyItemArguments",
                    Value = item.Arguments,
                }
            };

            await LoadItemThumbnail();

            return details;
        }

        public override async Task LoadAsync()
        {
            var details = await LoadPreviewAndDetails();
            Item.FileDetails?.Clear();
            Item.FileDetails = new System.Collections.ObjectModel.ObservableCollection<FileProperty>(details.Where(i => i.Value != null));
        }

        private async Task LoadItemThumbnail()
        {
            var (IconData, OverlayData, IsCustom) = await FileThumbnailHelper.LoadIconOverlayAsync(Item.ItemPath, 400);

            if (IconData != null)
            {
                Item.FileImage = await IconData.ToBitmapAsync();
            }
        }
    }
}