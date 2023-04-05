using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.Extensions.Logging;
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
using Windows.Foundation;
using Windows.Storage;

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

		public IShellPage AppInstance;
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
			PinToFavoritesCommand = new RelayCommand<WidgetCardItem>(PinToFavorites);
			UnpinFromFavoritesCommand = new RelayCommand<WidgetCardItem>(UnpinFromFavorites);
			OpenPropertiesCommand = new RelayCommand<WidgetCardItem>(OpenProperties);
		}

		public async Task OpenContextMenuAsync(FrameworkElement element, FileTagsItemViewModel item, Point position)
		{
			var flyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			flyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;

			App.Logger.LogWarning($"Item path: {item.Path} widgetcarditem.path = {item.Path}");
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path), item.IsFolder);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			if (!UserSettingsService.PreferencesSettingsService.MoveShellExtensionsToSubMenu)
				secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);
			secondaryElements.ForEach(flyout.SecondaryCommands.Add);
			ItemContextMenuFlyout = flyout;
			flyout.ShowAt(element, new FlyoutShowOptions { Position = position });

			await ShellContextmenuHelper.LoadShellMenuItems(item.Path, flyout, showOpenWithMenu: true, showSendToMenu: true);
		}

		private void OpenProperties(WidgetCardItem? item)
		{
			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = async (s, e) =>
			{
				ItemContextMenuFlyout.Closed -= flyoutClosed;
				ListedItem listedItem = new(null!)
				{
					ItemPath = (item.Item as FileTagsItemViewModel).Path,
					ItemNameRaw = (item.Item as FileTagsItemViewModel).Name,
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemType = "Folder".GetLocalizedResource(),
				};
				await FilePropertiesHelpers.OpenPropertiesWindowAsync(listedItem, AppInstance);
			};
			ItemContextMenuFlyout.Closed += flyoutClosed;
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
			App.Logger.LogWarning("rightTapped");
			if (sender is not StackPanel panel || panel.DataContext is not FileTagsItemViewModel item)
				return;

			e.Handled = true;
			Point position = e.GetPosition(panel);

			App.Logger.LogWarning($"Item path: {item.Path} widgetcarditem.path = {item.Path}");
			await OpenContextMenuAsync(panel, item, position);
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenItemsWithCaptionText".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith",
					},
					Tag = "OpenWithPlaceholder",
					ShowItem = !isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = !isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand,
					CommandParameter = item,
					ShowItem = !isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = userSettingsService.PreferencesSettingsService.ShowOpenInNewPane && isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "PinToFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconPinToFavorites",
					},
					Command = PinToFavoritesCommand,
					CommandParameter = item,
					ShowItem = !isPinned && isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconUnpinFromFavorites",
					},
					Command = UnpinFromFavoritesCommand,
					CommandParameter = item,
					ShowItem = isPinned && isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Properties".GetLocalizedResource(),
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
					Text = "Loading".GetLocalizedResource(),
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
