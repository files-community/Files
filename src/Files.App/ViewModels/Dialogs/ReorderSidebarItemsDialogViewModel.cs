// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	public sealed class ReorderSidebarItemsDialogViewModel : ObservableObject
	{
		private readonly IWindowsQuickAccessService quickAccessService = Ioc.Default.GetRequiredService<IWindowsQuickAccessService>();

		public string HeaderText = "ReorderSidebarItemsDialogText".GetLocalizedResource();
		public ICommand PrimaryButtonCommand { get; private set; }

		public ObservableCollection<LocationItem> SidebarPinnedFolderItems;

		public ReorderSidebarItemsDialogViewModel() 
		{
			//App.Logger.LogWarning(string.Join(", ", SidebarPinnedFolderItems.Select(x => x.Path)));
			PrimaryButtonCommand = new RelayCommand(SaveChanges);

			SidebarPinnedFolderItems =
				new(quickAccessService.PinnedFolderItems
					.Where(x => x is LocationItem loc && loc.Section is SectionType.Pinned && !loc.IsHeader)
					.Cast<LocationItem>());
		}

		public void SaveChanges()
		{
			quickAccessService.RefreshPinnedFolders(SidebarPinnedFolderItems.Select(x => x.Path).ToArray());
		}
	}
}
