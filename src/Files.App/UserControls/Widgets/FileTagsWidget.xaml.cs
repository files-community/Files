using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.Widgets.FileTagsWidget;
using Files.Shared.Extensions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.Widgets
{
	public sealed partial class FileTagsWidget : UserControl, IWidgetItemModel
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();


		public FileTagsWidgetViewModel ViewModel
		{
			get => (FileTagsWidgetViewModel)DataContext;
			set => DataContext = value;
		}

		public Func<string, Task>? OpenAction { get; set; }

		public string WidgetName => nameof(BundlesWidget);

		public string WidgetHeader => "FileTags".GetLocalizedResource();

		public string AutomationProperties => "FileTags".GetLocalizedResource();

		public bool IsWidgetSettingEnabled => UserSettingsService.PreferencesSettingsService.ShowFileTagsWidget;

		public bool ShowMenuFlyout => false;

		public MenuFlyoutItem? MenuFlyoutItem => null;

		public FileTagsWidget()
		{
			InitializeComponent();

			// Second function is layered on top to ensure that OpenPath function is late initialized and a null reference is not passed-in
			// See FileTagItemViewModel._openAction for more information
			ViewModel = new(x => OpenAction!(x));
		}

		private async void FileTagItem_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is FileTagsItemViewModel itemViewModel)
				await itemViewModel.ClickCommand.ExecuteAsync(null);
		}

		private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			if (sender is not StackPanel tagsItemsStackPanel || tagsItemsStackPanel.DataContext is not FileTagsItemViewModel item)
				return;

			var menuItems = GetTagItemMenuItems(item);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements.OfType<FrameworkElement>()
								.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width if the overflow menu setting is disabled

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(tagsItemsStackPanel, new FlyoutShowOptions { Position = e.GetPosition(tagsItemsStackPanel) });

			LoadShellMenuItems(item, itemContextMenuFlyout);

			e.Handled = true;
		}

		private async void LoadShellMenuItems(FileTagsItemViewModel item, CommandBarFlyout itemContextMenuFlyout)
		{
			try
			{
				var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
				var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(workingDir: null,
					new List<ListedItem>() { new ListedItem(null!) { ItemPath = item.Path } }, shiftPressed: shiftPressed, showOpenMenu: false, default);
				
				var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(shellMenuItems);
				if (itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") is not AppBarButton overflowItem)
					return;

				var flyoutItems = (overflowItem.Flyout as MenuFlyout)?.Items;
				if (flyoutItems is not null)
					overflowItems.ForEach(i => flyoutItems.Add(i));
				overflowItem.Visibility = overflowItems.Any() ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
			}
			catch { }
		}

		private List<ContextMenuFlyoutItemViewModel> GetTagItemMenuItems(FileTagsItemViewModel item)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ShowMoreOptions".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsHidden = true,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		public Task RefreshWidget()
		{
			return Task.CompletedTask;
		}

		public void Dispose()
		{
		}
	}
}
