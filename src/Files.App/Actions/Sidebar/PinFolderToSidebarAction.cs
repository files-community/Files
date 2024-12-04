// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;

namespace Files.App.Actions
{
	internal sealed class PinFolderToSidebarAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IQuickAccessService service = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string Label
			=> "PinFolderToSidebar".GetLocalizedResource();

		public string Description
			=> "PinFolderToSidebarDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.FavoritePin");

		public bool IsExecutable
			=> GetIsExecutable();

		public PinFolderToSidebarAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
			service.PinnedFoldersChanged += QuickAccessService_CollectionChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var items = context.HasSelection
				? context.SelectedItems.Select(x => x.ItemPath).ToArray()
				: context.Folder is not null
					? [context.Folder.ItemPath]
					: null;

			if (items is not null)
				await service.PinFolderAsync(items);
		}

		private bool GetIsExecutable()
		{
			string[] pinnedFolders = [.. service.QuickAccessFolders.Select(x => x.Path)];

			return context.HasSelection
				? context.SelectedItems.All(IsPinnable)
				: context.Folder is not null && IsPinnable(context.Folder);

			bool IsPinnable(ListedItem item)
			{
				return
					item.PrimaryItemAttribute is Windows.Storage.StorageItemTypes.Folder &&
					!pinnedFolders.Contains(item.ItemPath);
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

		private void QuickAccessService_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
