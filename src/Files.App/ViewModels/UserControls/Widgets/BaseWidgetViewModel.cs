// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers.ContextFlyouts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using WinRT;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents base ViewModel for widget ViewModels.
	/// </summary>
	public abstract class BaseWidgetViewModel : ObservableObject
	{
		// Dependency injections

		protected IWindowsRecentItemsService WindowsRecentItemsService { get; } = Ioc.Default.GetRequiredService<IWindowsRecentItemsService>();
		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();
		protected IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();
		protected IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();
		protected IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		protected DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();
		protected INetworkService NetworkService { get; } = Ioc.Default.GetRequiredService<INetworkService>();
		protected ICommandManager CommandManager { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		// Fields

		protected string? _flyoutItemPath;

		// Commands

		protected ICommand RemoveRecentItemCommand { get; set; } = null!;
		protected ICommand ClearAllItemsCommand { get; set; } = null!;
		protected ICommand OpenFileLocationCommand { get; set; } = null!;
		protected ICommand OpenPropertiesCommand { get; set; } = null!;
		protected ICommand PinToSidebarCommand { get; set; } = null!;
		protected ICommand UnpinFromSidebarCommand { get; set; } = null!;

		// Events

		public static event EventHandler<WidgetsRightClickedItemChangedEventArgs>? RightClickedItemChanged;

		// Methods

		public abstract List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false);

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		public void BuildItemContextMenu(object sender, RightTappedRoutedEventArgs e)
		{
			// Ensure values are not null
			if (sender is not FrameworkElement widgetCardItem ||
				widgetCardItem.DataContext is not WidgetCardItem item ||
				item.Path is null)
				return;

			// TODO: Add IsFolder to all WidgetCardItems
			// NOTE: This is a workaround for file tags isFolder
			var fileTagsCardItem = widgetCardItem.DataContext as WidgetFileTagCardItem;

			var flyout = new FastContextFlyout();
			_flyoutItemPath = item.Path;

			// Notify of the change on right clicked item
			OnRightClickedItemChanged(item, flyout.Flyout);

			// Build the widget's own items
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path), fileTagsCardItem is not null && fileTagsCardItem.IsFolder);
			flyout.Build(menuItems);

			// Convert the Open with / Send to placeholders to their submenu form up-front so the shell load can't
			// change item heights once the menu is visible; the loader fills their contents.
			flyout.ConvertPlaceholderToSubMenu("OpenWithPlaceholder", Strings.OpenWith.GetLocalizedResource(), "App.ThemedIcons.OpenWith");
			flyout.ConvertPlaceholderToSubMenu("SendToPlaceholder", Strings.SendTo.GetLocalizedResource(), null);

			// Pre-add "Show more options" before showing so filling it with shell items never resizes the menu
			var (moreOptions, moreSeparator) = flyout.AddShowMoreOptionsIfEnabled();

			// Show the flyout
			var invocationPosition = e.GetPosition(widgetCardItem);
			flyout.ResolvePlacement(widgetCardItem, invocationPosition);
			flyout.Flyout.ShowAt(widgetCardItem, new FlyoutShowOptions() { Position = invocationPosition });

			// Load the shell menu items
			_ = ShellContextFlyoutFactory.LoadShellMenuItemsAsync(_flyoutItemPath, flyout, null, moreOptions, moreSeparator, showOpenWithMenu: true, showSendToMenu: true);

			e.Handled = true;
		}

		// Command methods

		public virtual async Task ExecutePinToSidebarCommand(WidgetCardItem? item)
		{
			await QuickAccessService.PinToSidebarAsync(item?.Path ?? string.Empty);
		}

		public virtual async Task ExecuteUnpinFromSidebarCommand(WidgetCardItem? item)
		{
			await QuickAccessService.UnpinFromSidebarAsync(item?.Path ?? string.Empty);
		}

		protected void OnRightClickedItemChanged(WidgetCardItem? item, FlyoutBase? flyout)
		{
			RightClickedItemChanged?.Invoke(this, new WidgetsRightClickedItemChangedEventArgs(item, flyout));
		}
	}
}