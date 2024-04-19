// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class UnpinFolderFromSidebarAction : ObservableObject, IAction
	{
		private IContentPageContext context { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IWindowsQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IWindowsQuickAccessService>();

		public string Label
			=> "UnpinFolderFromSidebar".GetLocalizedResource();

		public string Description
			=> "UnpinFolderFromSidebarDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "Icons.Unpin.16x16");

		public bool IsExecutable
			=> GetIsExecutable();

		public UnpinFolderFromSidebarAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
			QuickAccessService.PinnedItemsChanged += QuickAccessService_DataChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.HasSelection)
			{
				var items = context.SelectedItems.Select(x => x.ItemPath).ToArray();
				await QuickAccessService.UnpinFolderFromSidebarAsync(items);
			}
			else if (context.Folder is not null)
			{
				await QuickAccessService.UnpinFolderFromSidebarAsync([context.Folder.ItemPath]);
			}
		}

		private bool GetIsExecutable()
		{
			string[] pinnedFolders = [.. QuickAccessService.PinnedFolderPaths];

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

		private void QuickAccessService_DataChanged(object? sender, ModifyQuickAccessEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
