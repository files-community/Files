// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Windows.Storage;

namespace Files.App.Actions
{
	internal class PinItemAction : ObservableObject, IExtendedAction
	{
		private readonly IContentPageContext context;

		private readonly IQuickAccessService service;

		public string Label
			=> "PinToFavorites".GetLocalizedResource();

		public string Description
			=> "PinItemToFavoritesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconPinToFavorites");

		public bool IsExecutable
			=> GetIsExecutable();

		public object? Parameter { get; set; }

		public PinItemAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			service = Ioc.Default.GetRequiredService<IQuickAccessService>();

			context.PropertyChanged += Context_PropertyChanged;
			App.QuickAccessManager.UpdateQuickAccessWidget += QuickAccessManager_DataChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.HasSelection)
			{
				var items = context.SelectedItems.Select(x => x.ItemPath).ToArray();

				await service.PinToSidebarAsync(items);
			}
			else if (context.Folder is not null)
			{
				await service.PinToSidebarAsync(context.Folder.ItemPath);
			}
			else if (Parameter is not null && Parameter is WidgetCardItem item)
			{
				await service.PinToSidebarAsync(item.Path);
			}
		}

		private bool GetIsExecutable()
		{
			string[] favorites = App.QuickAccessManager.Model.FavoriteItems.ToArray();

			if (context.HasSelection)
			{
				return context.SelectedItems.All(x => CanPin(x.ItemPath, x.PrimaryItemAttribute is StorageItemTypes.Folder));
			}
			else if (context.Folder is not null)
			{
				return CanPin(context.Folder.ItemPath, context.Folder.PrimaryItemAttribute is StorageItemTypes.Folder);
			}
			else if (Parameter is not null && Parameter is WidgetCardItem item)
			{
				var isFolder = NativeFileOperationsHelper.HasFileAttribute(item.Path, SystemIO.FileAttributes.Directory);
				return CanPin(item.Path, isFolder);
			}

			return false;

			bool CanPin(string path, bool isFolder)
			{
				return isFolder && !favorites.Contains(path);
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
