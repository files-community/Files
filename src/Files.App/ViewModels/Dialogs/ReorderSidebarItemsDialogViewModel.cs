using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.ServicesImplementation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	public class ReorderSidebarItemsDialogViewModel : ObservableObject
	{
		private readonly IQuickAccessService quickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string HeaderText = "ReorderSidebarItemsDialogText".GetLocalizedResource();
		public ICommand PrimaryButtonCommand { get; private set; }

		public ObservableCollection<LocationItem> SidebarFavoriteItems = new(App.QuickAccessManager.Model.favoriteList
			.Where(x => x is LocationItem loc && loc.Section is Filesystem.SectionType.Favorites && !loc.IsHeader)
			.Cast<LocationItem>());

		public ReorderSidebarItemsDialogViewModel() 
		{
			//App.Logger.Warn(string.Join(", ", SidebarFavoriteItems.Select(x => x.Path)));
			PrimaryButtonCommand = new RelayCommand(SaveChanges);
		}

		public void SaveChanges()
		{
			quickAccessService.Save(SidebarFavoriteItems.Select(x => x.Path).ToArray());
		}
	}
}
