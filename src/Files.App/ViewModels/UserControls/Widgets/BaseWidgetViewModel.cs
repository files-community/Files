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
	public abstract class BaseWidgetViewModel : ObservableObject
	{
		// Dependency injections

		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();
		protected IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		protected IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();
		protected IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();
		protected NetworkDrivesViewModel NetworkDrivesViewModel { get; } = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();
		protected DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();

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

		protected abstract List<ContextMenuFlyoutItemViewModel> GenerateRightClickContextMenu(WidgetCardItem item, bool isPinned, bool isFolder = false);

		// Event methods

		public void ShowRightClickContextMenu(object sender, RightTappedRoutedEventArgs e)
		{
			// Ensure values are not null
			if (sender is not FrameworkElement targetElement ||
				targetElement.DataContext is not WidgetCardItem item)
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
			var menuItems = GenerateRightClickContextMenu(item, QuickAccessService.IsItemPinned(item.Path ?? string.Empty));
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			// Set max width of the flyout
			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			// Add menu items to the secondary flyout
			secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

			// Show the flyout
			itemContextMenuFlyout.ShowAt(targetElement, new() { Position = e.GetPosition(targetElement) });

			// Load shell menu items
			_ = ShellContextmenuHelper.LoadShellMenuItemsAsync(_flyoutItemPath ?? string.Empty, itemContextMenuFlyout);
		}

		protected void OnRightClickedItemChanged(WidgetCardItem? item, CommandBarFlyout? flyout)
		{
			RightClickedItemChanged?.Invoke(this, new WidgetsRightClickedItemChangedEventArgs(item, flyout));
		}

		// Command methods

		protected async Task ExecuteOpenInNewTabCommand(WidgetCardItem? item)
		{
			await NavigationHelpers.OpenPathInNewTab(item!.Path);
		}

		protected async Task ExecuteOpenInNewWindowCommand(WidgetCardItem? item)
		{
			await NavigationHelpers.OpenPathInNewWindowAsync(item!.Path);
		}

		protected virtual async Task ExecutePinToFavoritesCommand(WidgetCardItem? item)
		{
			await QuickAccessService.PinToSidebarAsync(item!.Path ?? string.Empty);
		}

		protected virtual async Task ExecuteUnpinFromFavoritesCommand(WidgetCardItem? item)
		{
			await QuickAccessService.UnpinFromSidebarAsync(item!.Path ?? string.Empty);
		}
	}
}
