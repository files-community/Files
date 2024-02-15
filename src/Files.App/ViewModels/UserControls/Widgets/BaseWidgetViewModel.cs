// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

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

		// Events

		public static event EventHandler<WidgetsRightClickedItemChangedEventArgs>? RightClickedItemChanged;

		// Methods

		public void ShowContextFlyout(RightTappedRoutedEventArgs e, WidgetSectionType sectionType)
		{
			// Ensure values are not null
			if (e.OriginalSource is not FrameworkElement element ||
				element.DataContext is not WidgetCardItem item)
				return;

			// Create a new Flyout
			var itemContextMenuFlyout = new CommandBarFlyout() { Placement = FlyoutPlacementMode.Full };

			// Hook events
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			itemContextMenuFlyout.Closed += (sender, e) => RightClickedItemChanged?.Invoke(this, new(null, null));

			// Notify of the change on right clicked item
			RightClickedItemChanged?.Invoke(this, new(item, itemContextMenuFlyout));

			// Get items for the flyout
			var menuItems = sectionType switch
			{
				WidgetSectionType.QuickAccess => WidgetQuickAccessItemContextFlyoutFactory.Generate(),
				WidgetSectionType.Drive => WidgetDriveItemContextFlyoutFactory.Generate(),
				WidgetSectionType.FileTags => WidgetFileTagsItemContextFlyoutFactory.Generate(),
				WidgetSectionType.RecentItems => WidgetRecentItemContextFlyoutFactory.Generate(),
				_ => Enumerable.Empty<ContextMenuFlyoutItemViewModel>().ToList(),
			};

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
			_ = ShellContextFlyoutFactory.LoadShellMenuItemsAsync(item.Path ?? string.Empty, itemContextMenuFlyout);

			e.Handled = true;
		}
	}
}
