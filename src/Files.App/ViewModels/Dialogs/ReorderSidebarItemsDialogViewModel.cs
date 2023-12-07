﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
using Files.App.Services;
using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	public class ReorderSidebarItemsDialogViewModel : ObservableObject
	{
		private readonly IQuickAccessService quickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string HeaderText = "ReorderSidebarItemsDialogText".GetLocalizedResource();
		public ICommand PrimaryButtonCommand { get; private set; }

		public ObservableCollection<LocationItem> SidebarFavoriteItems = new(App.QuickAccessManager.Model.favoriteList
			.Where(x => x is LocationItem loc && loc.Section is SectionType.Favorites && !loc.IsHeader)
			.Cast<LocationItem>());

		public ReorderSidebarItemsDialogViewModel() 
		{
			//App.Logger.LogWarning(string.Join(", ", SidebarFavoriteItems.Select(x => x.Path)));
			PrimaryButtonCommand = new RelayCommand(SaveChanges);
		}

		public void SaveChanges()
		{
			quickAccessService.SaveAsync(SidebarFavoriteItems.Select(x => x.Path).ToArray());
		}
	}
}
