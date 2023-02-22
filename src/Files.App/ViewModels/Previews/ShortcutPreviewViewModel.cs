using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.ViewModels.Previews
{
	internal class ShortcutPreviewViewModel : BasePreviewModel
	{
		public ShortcutPreviewViewModel(ListedItem item) : base(item) { }

		public async override Task<List<FilePropertyViewModel>> LoadPreviewAndDetailsAsync()
		{
			var item = Item as ShortcutItem;
			var details = new List<FilePropertyViewModel>
			{
				GetFileProperty("PropertyItemPathDisplay", item.ItemPath),
				GetFileProperty("PropertyItemName", item.Name),
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
			Item.FileDetails = new(details.OfType<FilePropertyViewModel>());
		}

		private async Task LoadItemThumbnail()
		{
			var iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.ItemPath, 256);
			if (iconData is not null)
			{
				FileImage = await iconData.ToBitmapAsync();
			}
		}
	}
}
