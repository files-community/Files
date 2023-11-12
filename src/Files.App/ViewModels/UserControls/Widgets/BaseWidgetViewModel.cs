// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Files.Core.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public abstract class BaseWidgetViewModel
	{
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();
		public IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		public ICommand RemoveRecentItemCommand = null!;
		public ICommand ClearAllItemsCommand = null!;
		public ICommand OpenFileLocationCommand = null!;
		public ICommand OpenInNewTabCommand = null!;
		public ICommand OpenInNewWindowCommand = null!;
		public ICommand OpenPropertiesCommand = null!;
		public ICommand PinToFavoritesCommand = null!;
		public ICommand UnpinFromFavoritesCommand = null!;

		protected CommandBarFlyout ItemContextMenuFlyout = null!;

		protected string FlyoutItemPath = null!;

		public abstract List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false);

		public void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;

			if (sender is not Button widgetCardItem || widgetCardItem.DataContext is not WidgetCardItem item)
				return;

			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			ItemContextMenuFlyout = itemContextMenuFlyout;
			FlyoutItemPath = item.Path;
			ItemContextMenuFlyout.Opened += ItemContextMenuFlyout_Opened;
			itemContextMenuFlyout.ShowAt(widgetCardItem, new FlyoutShowOptions { Position = e.GetPosition(widgetCardItem) });

			e.Handled = true;
		}

		private async void ItemContextMenuFlyout_Opened(object? sender, object e)
		{
			ItemContextMenuFlyout.Opened -= ItemContextMenuFlyout_Opened;
			await ShellContextmenuHelper.LoadShellMenuItemsAsync(FlyoutItemPath, ItemContextMenuFlyout);
		}

		public async Task OpenInNewTabAsync(WidgetCardItem item)
		{
			await NavigationHelpers.OpenPathInNewTab(item.Path);
		}

		public async Task OpenInNewWindowAsync(WidgetCardItem item)
		{
			await NavigationHelpers.OpenPathInNewWindowAsync(item.Path);
		}

		public virtual async Task PinToFavoritesAsync(WidgetCardItem item)
		{
			_ = QuickAccessService.PinToSidebarAsync(item.Path);
		}

		public virtual async Task UnpinFromFavoritesAsync(WidgetCardItem item)
		{
			_ = QuickAccessService.UnpinFromSidebarAsync(item.Path);
		}
	}
}
