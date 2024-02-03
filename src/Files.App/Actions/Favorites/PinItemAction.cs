// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Windows.Storage;

namespace Files.App.Actions
{
	internal class PinItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string Label
			=> "PinToFavorites".GetLocalizedResource();

		public string Description
			=> "PinItemToFavoritesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconPinToFavorites");

		public bool IsExecutable
			=> GetIsExecutable();

		public PinItemAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
			App.QuickAccessManager.UpdateQuickAccessWidget += QuickAccessManager_DataChanged;
		}

		public async Task ExecuteAsync()
		{
			if (ContentPageContext.HasSelection)
			{
				var items = ContentPageContext.SelectedItems.Select(x => x.ItemPath).ToArray();

				await QuickAccessService.PinToSidebarAsync(items);
			}
			else if (ContentPageContext.Folder is not null)
			{
				await QuickAccessService.PinToSidebarAsync(ContentPageContext.Folder.ItemPath);
			}
		}

		private bool GetIsExecutable()
		{
			string[] favorites = App.QuickAccessManager.Model.FavoriteItems.ToArray();

			return ContentPageContext.HasSelection
				? ContentPageContext.SelectedItems.All(IsPinnable)
				: ContentPageContext.Folder is not null && IsPinnable(ContentPageContext.Folder);

			bool IsPinnable(ListedItem item)
			{
				return
					item.PrimaryItemAttribute is StorageItemTypes.Folder &&
					!favorites.Contains(item.ItemPath);
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.Folder):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void QuickAccessManager_DataChanged(object? sender, ModifyQuickAccessEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
