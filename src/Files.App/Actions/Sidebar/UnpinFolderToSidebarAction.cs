// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;

namespace Files.App.Actions
{
	internal sealed class UnpinFolderFromSidebarAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IQuickAccessService service = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string Label
			=> "UnpinFolderFromSidebar".GetLocalizedResource();

		public string Description
			=> "UnpinFolderFromSidebarDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.FavoritePinRemove");

		public bool IsExecutable
			=> GetIsExecutable();

		public UnpinFolderFromSidebarAction()
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
				await service.UnpinFolderAsync(items);
		}

		private bool GetIsExecutable()
		{
			string[] pinnedFolders = [.. service.PinnedFolders.Select(x => x.Path)];

			return context.HasSelection
				? context.SelectedItems.All(IsPinned)
				: context.Folder is not null && IsPinned(context.Folder);

			bool IsPinned(ListedItem item)
			{
				return pinnedFolders.Contains(item.ItemPath);
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
