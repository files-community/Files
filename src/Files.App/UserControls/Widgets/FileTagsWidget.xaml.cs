using CommunityToolkit.WinUI.UI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels;
using Files.App.ViewModels.Widgets;
using Files.App.Views;
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

namespace Files.App.UserControls.Widgets
{
	public sealed partial class FileTagsWidget : HomePageWidget, IWidgetItem
	{
		public FileTagsWidgetViewModel ViewModel
		{
			get => (FileTagsWidgetViewModel)DataContext;
			set => DataContext = value;
		}

		public Func<string, Task>? OpenAction { get; set; }

		public string WidgetName
			=> nameof(BundlesWidget);

		public string WidgetHeader
			=> "FileTags".GetLocalizedResource();

		public string AutomationProperties
			=> "FileTags".GetLocalizedResource();

		public bool IsWidgetSettingEnabled
			=> UserSettingsService.PreferencesSettingsService.ShowFileTagsWidget;

		public bool ShowMenuFlyout
			=> false;

		public MenuFlyoutItem? MenuFlyoutItem
			=> null;

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
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			if (sender is not StackPanel tagsItemsStackPanel || tagsItemsStackPanel.DataContext is not FileTagsItemViewModel item)
				return;
			itemContextMenuFlyout.ShowAt(tagsItemsStackPanel, new FlyoutShowOptions { Position = e.GetPosition(tagsItemsStackPanel) });
			LoadShellMenuItems(item.Path, itemContextMenuFlyout);

			e.Handled = true;
		}

		public async void LoadShellMenuItems(string item, CommandBarFlyout itemContextMenuFlyout)
		{
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(workingDir: null,
				new List<ListedItem>() { new ListedItem(null!) { ItemPath = item } }, shiftPressed: shiftPressed, showOpenMenu: false, default);
			try
			{
				var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(shellMenuItems);
				if (!secondaryElements.Any())
					return;

				var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(App.Window);
				var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");

				var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
				if (itemsControl is not null)
				{
					var maxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin;
					secondaryElements.OfType<FrameworkElement>()
									 .ForEach(x => x.MaxWidth = maxWidth); // Set items max width to current menu width (#5555)
				}
				
				secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			}
			catch { }
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned)
		{
			return new();
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
