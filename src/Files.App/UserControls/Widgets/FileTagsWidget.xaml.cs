using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels;
using Files.App.ViewModels.Widgets;
using Files.App.Views;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.Widgets
{
	public sealed partial class FileTagsWidget : HomePageWidget, IWidgetItemModel
	{
		public FileTagsWidgetViewModel ViewModel
		{
			get => (FileTagsWidgetViewModel)DataContext;
			set => DataContext = value;
		}

		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public Func<string, Task>? OpenAction { get; set; }

		public delegate void FileTagsOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public delegate void FileTagsNewPaneInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);

		public event FileTagsOpenLocationInvokedEventHandler FileTagsOpenLocationInvoked;
		public event FileTagsNewPaneInvokedEventHandler FileTagsNewPaneInvoked;

		public string WidgetName => nameof(BundlesWidget);

		public string WidgetHeader => "FileTags".GetLocalizedResource();

		public string AutomationProperties => "FileTags".GetLocalizedResource();

		public bool IsWidgetSettingEnabled => UserSettingsService.PreferencesSettingsService.ShowFileTagsWidget;

		public bool ShowMenuFlyout => false;

		public MenuFlyoutItem? MenuFlyoutItem => null;

		private ICommand OpenInNewPaneCommand;

		public FileTagsWidget()
		{
			InitializeComponent();

			// Second function is layered on top to ensure that OpenPath function is late initialized and a null reference is not passed-in
			// See FileTagItemViewModel._openAction for more information
			ViewModel = new(x => OpenAction!(x));
			OpenInNewTabCommand = new RelayCommand<WidgetCardItem>(OpenInNewTab);
			OpenInNewWindowCommand = new RelayCommand<WidgetCardItem>(OpenInNewWindow);
			OpenFileLocationCommand = new RelayCommand<WidgetCardItem>(OpenFileLocation);
			OpenInNewPaneCommand = new RelayCommand<WidgetCardItem>(OpenInNewPane);
		}

		private void OpenInNewPane(WidgetCardItem? item)
		{
			FileTagsNewPaneInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs()
			{
				Path = item?.Path ?? string.Empty
			});
		}

		private async void FileTagItem_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is FileTagsItemViewModel itemViewModel)
				await itemViewModel.ClickCommand.ExecuteAsync(null);
		}

		private async void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			App.Logger.Warn("rightTapped");
			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			if (sender is not StackPanel tagsItemsStackPanel || tagsItemsStackPanel.DataContext is not FileTagsItemViewModel item)
				return;

			App.Logger.Warn("Item path: " + item.Path + " widgetcarditem.path = " + (item as WidgetCardItem)?.Path);
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path), item.IsFolder);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			if (!UserSettingsService.PreferencesSettingsService.MoveShellExtensionsToSubMenu)
				secondaryElements.OfType<FrameworkElement>()
								 .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width if the overflow menu setting is disabled

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(tagsItemsStackPanel, new FlyoutShowOptions { Position = e.GetPosition(tagsItemsStackPanel) });

			await ShellContextmenuHelper.LoadShellMenuItems(item.Path, itemContextMenuFlyout);
			e.Handled = true;
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewTab/Text".GetLocalizedResource(),
					Glyph = "\uF113",
					GlyphFontFamilyName = "CustomGlyph",
					Command = OpenInNewTabCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewWindow/Text".GetLocalizedResource(),
					Glyph = "\uE737",
					Command = OpenInNewWindowCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RecentItemOpenFileLocation/Text".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand,
					CommandParameter = item,
					ShowItem = !isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseBackdropGlyph = "\uF056",
						BaseLayerGlyph = "\uF03B",
						OverlayLayerGlyph = "\uF03C",
					},
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = userSettingsService.PreferencesSettingsService.ShowOpenInNewPane && isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalizedResource(),
					Glyph = "\uE840",
					Command = PinToFavoritesCommand,
					CommandParameter = item,
					ShowItem = !isPinned && isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarUnpinFromFavorites/Text".GetLocalizedResource(),
					Glyph = "\uE77A",
					Command = UnpinFromFavoritesCommand,
					CommandParameter = item,
					ShowItem = isPinned && isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutContextFlyoutPropertiesFolder/Text".GetLocalizedResource(),
					Glyph = "\uE946",
					Command = OpenPropertiesCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "LoadingMoreOptions".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		public void OpenFileLocation(WidgetCardItem? item)
		{
			FileTagsOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = Directory.GetParent(item?.Path ?? string.Empty)?.FullName ?? string.Empty,
				ItemName = Path.GetFileName(item?.Path ?? string.Empty),
			});
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
