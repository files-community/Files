using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.Helpers.XamlHelpers;
using Files.App.ServicesImplementation;
using Files.App.ViewModels;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Widgets
{
	public class DriveCardItem : WidgetCardItem, IWidgetCardItem<DriveItem>, IComparable<DriveCardItem>
	{
		private BitmapImage thumbnail;
		private byte[] thumbnailData;

		public new DriveItem Item { get; private set; }
		public bool HasThumbnail => thumbnail is not null && thumbnailData is not null;
		public BitmapImage Thumbnail
		{
			get => thumbnail;
			set => SetProperty(ref thumbnail, value);
		}
		public DriveCardItem(DriveItem item)
		{
			Item = item;
			Path = item.Path;
		}

		public async Task LoadCardThumbnailAsync()
		{
			// Try load thumbnail using ListView mode
			if (thumbnailData is null || thumbnailData.Length == 0)
				thumbnailData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.Path, Convert.ToUInt32(Constants.Widgets.WidgetIconSize), Windows.Storage.FileProperties.ThumbnailMode.SingleItem);

			// Thumbnail is still null, use DriveItem icon (loaded using SingleItem mode)
			if (thumbnailData is null || thumbnailData.Length == 0)
				thumbnailData = Item.IconData;

			// Thumbnail data is valid, set the item icon
			if (thumbnailData is not null && thumbnailData.Length > 0)
				Thumbnail = await App.Window.DispatcherQueue.EnqueueAsync(() => thumbnailData.ToBitmapAsync(Constants.Widgets.WidgetIconSize));
		}

		public int CompareTo(DriveCardItem? other) => Item.Path.CompareTo(other?.Item?.Path);
	}

	public sealed partial class DrivesWidget : HomePageWidget, IWidgetItemModel, INotifyPropertyChanged
	{		
		public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

		public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;

		public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

		public event DrivesWidgetNewPaneInvokedEventHandler DrivesWidgetNewPaneInvoked;

		public event PropertyChangedEventHandler? PropertyChanged;

		public static ObservableCollection<DriveCardItem> ItemsAdded = new();

		private IShellPage associatedInstance;

		public ICommand EjectDeviceCommand;
		public ICommand DisconnectNetworkDriveCommand;
		public ICommand GoToStorageSenseCommand;
		public ICommand OpenInNewPaneCommand;

		public IShellPage AppInstance
		{
			get => associatedInstance;
			set
			{
				if (value != associatedInstance)
				{
					associatedInstance = value;
					NotifyPropertyChanged(nameof(AppInstance));
				}
			}
		}

		public string WidgetName => nameof(DrivesWidget);

		public string AutomationProperties => "DrivesWidgetAutomationProperties/Name".GetLocalizedResource();

		public string WidgetHeader => "Drives".GetLocalizedResource();

		public bool IsWidgetSettingEnabled => UserSettingsService.PreferencesSettingsService.ShowDrivesWidget;

		public bool ShowMenuFlyout => true;

		public MenuFlyoutItem MenuFlyoutItem => new MenuFlyoutItem()
		{
			Icon = new FontIcon() { Glyph = "\uE710" },
			Text = "DrivesWidgetOptionsFlyoutMapNetDriveMenuItem/Text".GetLocalizedResource(),
			Command = MapNetworkDriveCommand
		};

		public AsyncRelayCommand MapNetworkDriveCommand { get; }

		public DrivesWidget()
		{
			InitializeComponent();

			Manager_DataChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			App.DrivesManager.DataChanged += Manager_DataChanged;

			EjectDeviceCommand = new AsyncRelayCommand<DriveCardItem>(EjectDevice);
			OpenInNewTabCommand = new RelayCommand<WidgetCardItem>(OpenInNewTab);
			OpenInNewWindowCommand = new RelayCommand<WidgetCardItem>(OpenInNewWindow);
			OpenInNewPaneCommand = new AsyncRelayCommand<DriveCardItem>(OpenInNewPane);
			OpenPropertiesCommand = new AsyncRelayCommand<DriveCardItem>(OpenProperties);
			PinToFavoritesCommand = new RelayCommand<WidgetCardItem>(PinToFavorites);
			UnpinFromFavoritesCommand = new RelayCommand<WidgetCardItem>(UnpinFromFavorites);
			MapNetworkDriveCommand = new AsyncRelayCommand(DoNetworkMapDrive); 
			DisconnectNetworkDriveCommand = new RelayCommand<DriveCardItem>(DisconnectNetworkDrive);
		}
		
		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned)
		{
			var options = (item.Item as DriveItem)?.MenuOptions;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewPane/Text".GetLocalizedResource(),
					Glyph = "\uF117",
					GlyphFontFamilyName = "CustomGlyph",
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = ShowMultiPaneControls
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewTab/Text".GetLocalizedResource(),
					Glyph = "\uF113",
					GlyphFontFamilyName = "CustomGlyph",
					Command = OpenInNewTabCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewWindow/Text".GetLocalizedResource(),
					Glyph = "\uE737",
					Command = OpenInNewWindowCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalizedResource(),
					Glyph = "\uE840",
					Command = PinToFavoritesCommand,
					CommandParameter = item,
					ShowItem = !isPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarUnpinFromFavorites/Text".GetLocalizedResource(),
					Glyph = "\uE77A",
					Command = UnpinFromFavoritesCommand,
					CommandParameter = item,
					ShowItem = isPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarEjectDevice/Text".GetLocalizedResource(),
					Glyph = "\uF10B",
					GlyphFontFamilyName = "CustomGlyph",
					Command = EjectDeviceCommand,
					CommandParameter = item,
					ShowItem = options?.ShowEjectDevice ?? false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutContextFlyoutPropertiesFolder/Text".GetLocalizedResource(),
					Glyph = "\uE946",
					Command = OpenPropertiesCommand,
					CommandParameter = item
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

		private async Task DoNetworkMapDrive()
		{
			await NetworkDrivesManager.OpenMapNetworkDriveDialogAsync(NativeWinApiHelper.CoreWindowHandle.ToInt64());
		}
		
		private async void Manager_DataChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await DispatcherQueue.EnqueueAsync(async () =>
			{
				foreach (DriveItem drive in App.DrivesManager.Drives)
				{
					if (!ItemsAdded.Any(x => x.Item == drive) && drive.Type != DriveType.VirtualDrive)
					{
						var cardItem = new DriveCardItem(drive);
						ItemsAdded.AddSorted(cardItem);
						await cardItem.LoadCardThumbnailAsync(); // After add
					}
				}

				foreach (DriveCardItem driveCard in ItemsAdded.ToList())
				{
					if (!App.DrivesManager.Drives.Contains(driveCard.Item))
						ItemsAdded.Remove(driveCard);
				}
			});
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private async Task EjectDevice(DriveCardItem item)
		{
			var result = await DriveHelpers.EjectDeviceAsync(item.Item.Path);
			await UIHelpers.ShowDeviceEjectResultAsync(result);
		}


		private async Task OpenProperties(DriveCardItem item)
		{ 
			await FilePropertiesHelpers.OpenPropertiesWindowAsync(item.Item, associatedInstance); 
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			string ClickedCard = (sender as Button).Tag.ToString();
			string NavigationPath = ClickedCard; // path to navigate

			if (await DriveHelpers.CheckEmptyDrive(NavigationPath))
				return;

			var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(NavigationPath);
				return;
			}

			DrivesWidgetInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
			{
				Path = NavigationPath
			});
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed) // check middle click
				return;
			string navigationPath = (sender as Button).Tag.ToString();
			if (await DriveHelpers.CheckEmptyDrive(navigationPath))
				return;
			await NavigationHelpers.OpenPathInNewTab(navigationPath);
		}

		public class DrivesWidgetInvokedEventArgs : EventArgs
		{
			public string Path { get; set; }
		}

		public bool ShowMultiPaneControls
		{
			get => AppInstance.PaneHolder?.IsMultiPaneEnabled ?? false;
		}

		private async Task OpenInNewPane(DriveCardItem item)
		{
			if (await DriveHelpers.CheckEmptyDrive(item.Item.Path))
				return;
			DrivesWidgetNewPaneInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
			{
				Path = item.Item.Path
			});
		}

		private void MenuFlyout_Opening(object sender, object e)
		{
			var newPaneMenuItem = (sender as MenuFlyout).Items.Single(x => x.Name == "OpenInNewPane");
			newPaneMenuItem.Visibility = ShowMultiPaneControls ? Visibility.Visible : Visibility.Collapsed;

			var pinToFavoritesItem = (sender as MenuFlyout).Items.Single(x => x.Name == "PinToFavorites");
			pinToFavoritesItem.Visibility = (pinToFavoritesItem.DataContext as DriveItem).IsPinned ? Visibility.Collapsed : Visibility.Visible;

			var unpinFromFavoritesItem = (sender as MenuFlyout).Items.Single(x => x.Name == "UnpinFromFavorites");
			unpinFromFavoritesItem.Visibility = (unpinFromFavoritesItem.DataContext as DriveItem).IsPinned ? Visibility.Visible : Visibility.Collapsed;
		}

		private void DisconnectNetworkDrive(DriveCardItem item)
		{
			NetworkDrivesManager.DisconnectNetworkDrive(item.Item.Path);
		}

		private void GoToStorageSense_Click(object sender, RoutedEventArgs e)
		{
			string clickedCard = (sender as Button).Tag.ToString();
			StorageSenseHelper.OpenStorageSense(clickedCard);
		}

		public async Task RefreshWidget()
		{
			var updateTasks = ItemsAdded.Select(item => item.Item.UpdatePropertiesAsync());
			await Task.WhenAll(updateTasks);
		}

		public void Dispose()
		{

		}
	}
}
