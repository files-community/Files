// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Files.App.ServicesImplementation;
using Files.Sdk.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;

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

		public void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			if (sender is not Button widgetCardItem || widgetCardItem.DataContext is not WidgetCardItem item)
				return;

			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements.OfType<FrameworkElement>()
							 .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			ItemContextMenuFlyout = itemContextMenuFlyout;
			itemContextMenuFlyout.ShowAt(widgetCardItem, new FlyoutShowOptions { Position = e.GetPosition(widgetCardItem) });

			_ = ShellContextmenuHelper.LoadShellMenuItems(item.Path, itemContextMenuFlyout);

			e.Handled = true;
		}


		public async Task OpenInNewTab(WidgetCardItem item)
		{
			await NavigationHelpers.OpenPathInNewTab(item.Path);
		}

		public async Task OpenInNewWindow(WidgetCardItem item)
		{
			await NavigationHelpers.OpenPathInNewWindowAsync(item.Path);
		}

		public virtual async Task PinToFavorites(WidgetCardItem item)
		{
			_ = QuickAccessService.PinToSidebar(item.Path);
		}

		public virtual async Task UnpinFromFavorites(WidgetCardItem item)
		{
			_ = QuickAccessService.UnpinFromSidebar(item.Path);
		}

	}
}
