using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ServicesImplementation;
using Files.App.ViewModels;
using Files.Backend.Services.Settings;
using Files.Sdk.Storage;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;

namespace Files.App.UserControls.Widgets
{
	public abstract class HomePageWidget : UserControl
	{
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();
		public IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		public ICommand RemoveRecentItemCommand;
		public ICommand ClearAllItemsCommand;
		public ICommand OpenFileLocationCommand;
		public ICommand OpenInNewTabCommand;
		public ICommand OpenInNewWindowCommand;
		public ICommand OpenPropertiesCommand;
		public ICommand PinToFavoritesCommand;
		public ICommand UnpinFromFavoritesCommand;

		protected CommandBarFlyout ItemContextMenuFlyout;

		public abstract List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false);

		public async Task OpenContextMenuAsync(FrameworkElement element, Point position)
		{
			if (element.DataContext is not WidgetCardItem item)
				return;

			var flyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			flyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;

			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);
			secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);
			secondaryElements.ForEach(flyout.SecondaryCommands.Add);

			ItemContextMenuFlyout = flyout;
			flyout.ShowAt(element, new FlyoutShowOptions { Position = position });
			await ShellContextmenuHelper.LoadShellMenuItems(item.Path, flyout);
		}

		protected async void OpenInNewTab(WidgetCardItem item)
		{
			await NavigationHelpers.OpenPathInNewTab(item.Path);
		}

		protected async void OpenInNewWindow(WidgetCardItem item)
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

		protected async void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element)
			{
				Point position = e.GetPosition(element);
				await OpenContextMenuAsync(element, position);
				e.Handled = true;
			}
		}
	}
}
