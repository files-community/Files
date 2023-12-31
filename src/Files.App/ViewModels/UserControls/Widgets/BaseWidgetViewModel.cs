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
	/// <summary>
	/// Represents base ViewModel for Widget ViewModels.
	/// </summary>
	public abstract class BaseWidgetViewModel
	{
		// Dependency injections

		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();
		protected IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();
		protected IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		protected IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();
		protected DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();
		protected NetworkDrivesViewModel NetworkDrivesViewModel { get; } = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();

		// Fields

		protected string? _flyoutItemPath;

		// Commands

		public ICommand? RemoveRecentItemCommand { get; protected set; }
		public ICommand? ClearAllItemsCommand { get; protected set; }
		public ICommand? OpenFileLocationCommand { get; protected set; }
		public ICommand? OpenInNewTabCommand { get; protected set; }
		public ICommand? OpenInNewWindowCommand { get; protected set; }
		public ICommand? OpenPropertiesCommand { get; protected set; }
		public ICommand? PinToFavoritesCommand { get; protected set; }
		public ICommand? UnpinFromFavoritesCommand { get; protected set; }

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
			itemContextMenuFlyout.Opened += (sender, e) => OnRightClickedItemChanged(null, null);

			_flyoutItemPath = item.Path;

			// Notify of the change on right clicked item
			OnRightClickedItemChanged(item, itemContextMenuFlyout);

			// Get items for the flyout
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			// Set max width of the flyout
			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			// Add menu items to the secondary flyout
			secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

			// Show the flyout
			itemContextMenuFlyout.ShowAt(widgetCardItem, new() { Position = e.GetPosition(widgetCardItem) });

			// Load shell menu items
			_ = ShellContextmenuHelper.LoadShellMenuItemsAsync(_flyoutItemPath, itemContextMenuFlyout);

			e.Handled = true;
		}

		// Command methods

		protected async Task ExecuteOpenInNewTabCommand(WidgetCardItem? item)
		{
			await NavigationHelpers.OpenPathInNewTab(item.Path);
		}

		protected async Task ExecuteOpenInNewWindowCommand(WidgetCardItem? item)
		{
			await NavigationHelpers.OpenPathInNewWindowAsync(item.Path);
		}

		protected virtual async Task ExecutePinToFavoritesCommand(WidgetCardItem? item)
		{
			await QuickAccessService.PinToSidebarAsync(item.Path);
		}

		protected virtual async Task ExecuteUnpinFromFavoritesCommand(WidgetCardItem? item)
		{
			await QuickAccessService.UnpinFromSidebarAsync(item.Path);
		}

		protected void OnRightClickedItemChanged(WidgetCardItem? item, CommandBarFlyout? flyout)
		{
			RightClickedItemChanged?.Invoke(this, new WidgetsRightClickedItemChangedEventArgs(item, flyout));
		}
	}
}
