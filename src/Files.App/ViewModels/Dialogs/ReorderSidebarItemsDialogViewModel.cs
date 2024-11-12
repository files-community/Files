// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	public sealed class ReorderSidebarItemsDialogViewModel : ObservableObject
	{
		private readonly IQuickAccessService QuickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string HeaderText = "ReorderSidebarItemsDialogText".GetLocalizedResource();
		public ObservableCollection<LocationItem> SidebarPinnedFolderItems { get; }

		public ICommand PrimaryButtonCommand { get; private set; }

		public ReorderSidebarItemsDialogViewModel() 
		{
			PrimaryButtonCommand = new RelayCommand(SaveChanges);

			SidebarPinnedFolderItems = new(QuickAccessService.PinnedFolders
				.Where(x => x is LocationItem loc && loc.Section is SectionType.Pinned && !loc.IsHeader)
				.Cast<LocationItem>());
		}

		public void SaveChanges()
		{
			// TODO: Fire the reset event
			//QuickAccessService.SaveAsync(SidebarPinnedFolderItems.Select(x => x.Path).ToArray());
		}
	}
}
