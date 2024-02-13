// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents base ViewModel for widget ViewModels.
	/// </summary>
	public abstract class BaseWidgetViewModel : UserControl
	{
		// Dependency injections

		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();
		public IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		// Fields

		protected string? _flyoutItemPath;

		// Commands

		protected ICommand? RemoveRecentItemCommand { get; set; }
		protected ICommand? ClearAllItemsCommand { get; set; }
		protected ICommand? OpenFileLocationCommand { get; set; }
		protected ICommand? OpenInNewTabCommand { get; set; }
		protected ICommand? OpenInNewWindowCommand { get; set; }
		protected ICommand? OpenPropertiesCommand { get; set; }
		protected ICommand? PinToFavoritesCommand { get; set; }
		protected ICommand? UnpinFromFavoritesCommand { get; set; }

		// Events

		public static event EventHandler<WidgetsRightClickedItemChangedEventArgs>? RightClickedItemChanged;

		// Abstract methods

		public abstract List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false);

		// Event methods

		public void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			// Ensure values are not null
			if (sender is not Button widgetCardItem ||
				widgetCardItem.DataContext is not WidgetCardItem item)
				return;

			// Create a new Flyout
			var itemContextMenuFlyout = new CommandBarFlyout()
			{
				Placement = FlyoutPlacementMode.Full
			};

			// Hook events
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			itemContextMenuFlyout.Closed += (sender, e) => OnRightClickedItemChanged(null, null);

			_flyoutItemPath = item.Path;

			// Notify of the change on right clicked item
			OnRightClickedItemChanged(item, itemContextMenuFlyout);

			// Get items for the flyout
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
			var (_, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(menuItems);

			// Set max width of the flyout
			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			// Add menu items to the secondary flyout
			secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

			// Show the flyout
			itemContextMenuFlyout.ShowAt(widgetCardItem, new() { Position = e.GetPosition(widgetCardItem) });

			// Load shell menu items
			_ = ShellContextFlyoutFactory.LoadShellMenuItemsAsync(_flyoutItemPath, itemContextMenuFlyout);

			e.Handled = true;
		}

		// Command methods

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
			await QuickAccessService.PinToSidebarAsync(item.Path);
		}

		public virtual async Task UnpinFromFavoritesAsync(WidgetCardItem item)
		{
			await QuickAccessService.UnpinFromSidebarAsync(item.Path);
		}

		protected void OnRightClickedItemChanged(WidgetCardItem? item, CommandBarFlyout? flyout)
		{
			RightClickedItemChanged?.Invoke(this, new WidgetsRightClickedItemChangedEventArgs(item, flyout));
		}
	}
}
