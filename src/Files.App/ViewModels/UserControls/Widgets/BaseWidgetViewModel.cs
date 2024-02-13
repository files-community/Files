// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents base ViewModel for widget ViewModels.
	/// </summary>
	public abstract class BaseWidgetViewModel
	{
		// Dependency injections

		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		public IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();
		public IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		public IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		// Fields

		protected string? _flyoutItemPath;

		// Commands

		protected ICommand RemoveRecentItemCommand { get; set; } = null!;
		protected ICommand ClearAllItemsCommand { get; set; } = null!;
		protected ICommand OpenFileLocationCommand { get; set; } = null!;
		protected ICommand OpenInNewTabCommand { get; set; } = null!;
		protected ICommand OpenInNewWindowCommand { get; set; } = null!;
		protected ICommand OpenPropertiesCommand { get; set; } = null!;
		protected ICommand PinToFavoritesCommand { get; set; } = null!;
		protected ICommand UnpinFromFavoritesCommand { get; set; } = null!;

		// Events

		public static event EventHandler<WidgetsRightClickedItemChangedEventArgs>? RightClickedItemChanged;

		// Abstract methods

		public abstract List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false);

		public void BuildContextFlyout(object sender, RightTappedRoutedEventArgs e)
		{
			// Ensure values are not null
			if (sender is not FrameworkElement element ||
				element.DataContext is not WidgetCardItem item)
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
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path ?? string.Empty));
			var (_, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(menuItems);

			// Set max width of the flyout
			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			// Add menu items to the secondary flyout
			secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

			// Show the flyout
			itemContextMenuFlyout.ShowAt(element, new() { Position = e.GetPosition(element) });

			// Load shell menu items
			_ = ShellContextFlyoutFactory.LoadShellMenuItemsAsync(_flyoutItemPath ?? string.Empty, itemContextMenuFlyout);

			e.Handled = true;
		}

		// Command methods

		public async Task OpenInNewTabAsync(WidgetCardItem? item)
		{
			if (item is null)
				return;

			await NavigationHelpers.OpenPathInNewTab(item.Path);
		}

		public async Task OpenInNewWindowAsync(WidgetCardItem? item)
		{
			if (item is null)
				return;

			await NavigationHelpers.OpenPathInNewWindowAsync(item.Path);
		}

		public virtual async Task PinToFavoritesAsync(WidgetCardItem? item)
		{
			if (item is null || string.IsNullOrEmpty(item.Path))
				return;

			await QuickAccessService.PinToSidebarAsync(item.Path);
		}

		public virtual async Task UnpinFromFavoritesAsync(WidgetCardItem? item)
		{
			if (item is null || string.IsNullOrEmpty(item.Path))
				return;

			await QuickAccessService.UnpinFromSidebarAsync(item.Path);
		}

		protected void OnRightClickedItemChanged(WidgetCardItem? item, CommandBarFlyout? flyout)
		{
			RightClickedItemChanged?.Invoke(this, new(item, flyout));
		}
	}
}
