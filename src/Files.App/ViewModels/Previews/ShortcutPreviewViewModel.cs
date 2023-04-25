// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;

namespace Files.App.ViewModels.Previews
{
	internal class ShortcutPreviewViewModel : BasePreviewModel
	{
		public ShortcutPreviewViewModel(ListedItem item) : base(item) { }

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var item = Item as ShortcutItem;
			var details = new List<FileProperty>
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
			Item.FileDetails = new(details.OfType<FileProperty>());
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
