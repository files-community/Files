// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;

namespace Files.App.Actions
{
	internal sealed class PinFolderToSidebarAction : ObservableObject, IAction
	{
		private IContentPageContext context { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IWindowsQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IWindowsQuickAccessService>();

		public string Label
			=> "PinFolderToSidebar".GetLocalizedResource();

		public string Description
			=> "PinFolderToSidebarDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "Icons.Pin.16x16");

		public bool IsExecutable
			=> GetIsExecutable();

		public PinFolderToSidebarAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
			QuickAccessService.PinnedItemsChanged += QuickAccessManager_DataChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.HasSelection)
			{
				var items = context.SelectedItems.Select(x => x.ItemPath).ToArray();

				await QuickAccessService.PinFolderAsync(items);
			}
			else if (context.Folder is not null)
			{
				await QuickAccessService.PinFolderAsync([context.Folder.ItemPath]);
			}
		}

		private bool GetIsExecutable()
		{
			string[] pinnedFolders = [.. QuickAccessService.PinnedFolderPaths];

			return context.HasSelection
				? context.SelectedItems.All(IsPinnable)
				: context.Folder is not null && IsPinnable(context.Folder);

			bool IsPinnable(ListedItem item)
			{
				return
					item.PrimaryItemAttribute is StorageItemTypes.Folder &&
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

		private void QuickAccessManager_DataChanged(object? sender, ModifyQuickAccessEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
