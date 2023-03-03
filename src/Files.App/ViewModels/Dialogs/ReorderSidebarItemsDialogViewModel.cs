using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.ServicesImplementation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	public class ReorderSidebarItemsDialogViewModel : ObservableObject
	{
		private readonly IQuickAccessService quickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string HeaderText = "ReorderSidebarItemsDialogText".GetLocalizedResource();
		public ICommand PrimaryButtonCommand { get; private set; }

		public ObservableCollection<LocationItem> SidebarFavoriteItems = new ObservableCollection<LocationItem>(App.QuickAccessManager.Model.favoriteList
			.Where(x => x is LocationItem loc && loc.Section is Filesystem.SectionType.Favorites && !loc.IsHeader)
			.Cast<LocationItem>());

		public ReorderSidebarItemsDialogViewModel() 
		{
			PrimaryButtonCommand = new RelayCommand(SaveChanges);
		}

		public void SaveChanges()
		{
			quickAccessService.Save(SidebarFavoriteItems.Select(x => x.Path).ToArray());
		}
	}
}
