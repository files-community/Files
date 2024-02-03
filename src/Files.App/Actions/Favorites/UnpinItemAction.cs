// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;

namespace Files.App.Actions
{
	internal class UnpinItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string Label
			=> "UnpinFromFavorites".GetLocalizedResource();

		public string Description
			=> "UnpinItemFromFavoritesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconUnpinFromFavorites");

		public bool IsExecutable
			=> GetIsExecutable();

		public UnpinItemAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
			App.QuickAccessManager.UpdateQuickAccessWidget += QuickAccessManager_DataChanged;
		}

		public async Task ExecuteAsync()
		{
			if (ContentPageContext.HasSelection)
			{
				var items = ContentPageContext.SelectedItems.Select(x => x.ItemPath).ToArray();
				await QuickAccessService.UnpinFromSidebarAsync(items);
			}
			else if (ContentPageContext.Folder is not null)
			{
				await QuickAccessService.UnpinFromSidebarAsync(ContentPageContext.Folder.ItemPath);
			}
		}

		private bool GetIsExecutable()
		{
			string[] favorites = App.QuickAccessManager.Model.FavoriteItems.ToArray();

			return ContentPageContext.HasSelection
				? ContentPageContext.SelectedItems.All(IsPinned)
				: ContentPageContext.Folder is not null && IsPinned(ContentPageContext.Folder);

			bool IsPinned(ListedItem item)
			{
				return favorites.Contains(item.ItemPath);
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
