using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Files.Common;

namespace Files.ViewModels.Previews
{
    class ShortcutPreviewViewModel : BasePreviewModel
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
                    LocalizedName = "Item path",
                    Value = item.ItemPath,
                },
                new FileProperty()
                {
                    LocalizedName = "Item name",
                    Value = item.ItemName,
                },
                new FileProperty()
                {
                    LocalizedName = "Type",
                    Value = item.ItemType,
                },
                new FileProperty()
                {
                    LocalizedName = "Target Path",
                    Value = item.TargetPath,
                },
                new FileProperty()
                {
                    LocalizedName = "Arguments",
                    Value = item.Arguments,
                }
            };

            _ = await base.LoadPreviewAndDetails();

            //await base.LoadPreviewAndDetails();
            return details;
        }

        public override async Task LoadAsync()
        {
            var details = await LoadPreviewAndDetails();
            Item.FileDetails?.Clear();
            Item.FileDetails = new System.Collections.ObjectModel.ObservableCollection<FileProperty>(details.Where(i => i.Value != null));
        }
    }
}
