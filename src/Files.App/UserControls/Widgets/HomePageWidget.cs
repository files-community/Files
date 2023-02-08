using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ServicesImplementation;
using Files.App.ServicesImplementation.Settings;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.UserControls.Widgets
{
	public abstract class HomePageWidget : UserControl
	{
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public ICommand RemoveRecentItemCommand;
		public ICommand ClearAllItemsCommand;
		public ICommand OpenFileLocationCommand;
		public ICommand OpenInNewTabCommand;
		public ICommand OpenInNewWindowCommand;
		public ICommand OpenPropertiesCommand;
		public ICommand PinToFavoritesCommand;
		public ICommand UnpinFromFavoritesCommand;

		public async void OpenInNewTab(WidgetCardItem item)
		{
			await NavigationHelpers.OpenPathInNewTab(item.Path);
		}

		public async void OpenInNewWindow(WidgetCardItem item)
		{
			await NavigationHelpers.OpenPathInNewWindowAsync(item.Path);
		}

		public virtual async void PinToFavorites(WidgetCardItem item)
		{
			_ = QuickAccessService.PinToSidebar(item.Path);
		}

		public virtual async void UnpinFromFavorites(WidgetCardItem item)
		{
			_ = QuickAccessService.UnpinFromSidebar(item.Path);
		}

	}
}
