using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Shell;
using Files.App.ViewModels;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
	public sealed partial class DrivesWidget : BaseWidgetControl, IWidgetItem, INotifyPropertyChanged
	{
		#region Fields and Properties
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public string WidgetName
			=> nameof(DrivesWidget);

		public string AutomationProperties
			=> "DrivesWidgetAutomationProperties/Name".GetLocalizedResource();

		public string WidgetHeader
			=> "Drives".GetLocalizedResource();

		public bool IsWidgetSettingEnabled
			=> base.UserSettingsService.PreferencesSettingsService.ShowDrivesWidget;

		public bool ShowMenuFlyout
			=> true;

		public MenuFlyoutItem MenuFlyoutItem => new()
		{
			Icon = new FontIcon() { Glyph = "\uE710" },
			Text = "DrivesWidgetOptionsFlyoutMapNetDriveMenuItem/Text".GetLocalizedResource(),
			Command = MapNetworkDriveCommand
		};

		public ObservableCollection<DriveCardItem> DriveItems = new();

		private IShellPage _AppInstance;
		public IShellPage AppInstance
		{
			get => _AppInstance;
			set
			{
				if (value != _AppInstance)
				{
					_AppInstance = value;
					NotifyPropertyChanged(nameof(AppInstance));
				}
			}
		}

		public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;

		public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public event DrivesWidgetNewPaneInvokedEventHandler DrivesWidgetNewPaneInvoked;

		public event PropertyChangedEventHandler? PropertyChanged;

		public ICommand FormatDriveCommand;
		public ICommand EjectDeviceCommand;
		public ICommand DisconnectNetworkDriveCommand;
		public ICommand GoToStorageSenseCommand;
		public ICommand OpenInNewPaneCommand;

		public AsyncRelayCommand MapNetworkDriveCommand { get; }
		#endregion

		public DrivesWidget()
		{
			InitializeComponent();

			Manager_DataChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			App.DrivesManager.DataChanged += Manager_DataChanged;

			FormatDriveCommand = new RelayCommand<DriveCardItem>(FormatDrive);
			EjectDeviceCommand = new AsyncRelayCommand<DriveCardItem>(EjectDevice);
			OpenInNewTabCommand = new RelayCommand<WidgetCardItem>(OpenInNewTab);
			OpenInNewWindowCommand = new RelayCommand<WidgetCardItem>(OpenInNewWindow);
			OpenInNewPaneCommand = new AsyncRelayCommand<DriveCardItem>(OpenInNewPane);
			OpenPropertiesCommand = new RelayCommand<DriveCardItem>(OpenProperties);
			PinToFavoritesCommand = new RelayCommand<WidgetCardItem>(PinToFavorites);
			UnpinFromFavoritesCommand = new RelayCommand<WidgetCardItem>(UnpinFromFavorites);
			MapNetworkDriveCommand = new AsyncRelayCommand(DoNetworkMapDrive); 
			DisconnectNetworkDriveCommand = new RelayCommand<DriveCardItem>(DisconnectNetworkDrive);
		}

		#region Methods
		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned)
		{
			var drive = DriveItems.Where(x => string.Equals(PathNormalization.NormalizePath(x.Path), PathNormalization.NormalizePath(item.Path), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
			var options = drive?.Item.MenuOptions;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewTab/Text".GetLocalizedResource(),
					Glyph = "\uF113",
					GlyphFontFamilyName = "CustomGlyph",
					Command = OpenInNewTabCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.PreferencesSettingsService.ShowOpenInNewTab
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewWindow/Text".GetLocalizedResource(),
					Glyph = "\uE737",
					Command = OpenInNewWindowCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.PreferencesSettingsService.ShowOpenInNewWindow
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
					ShowItem = UserSettingsService.PreferencesSettingsService.ShowOpenInNewPane
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
					Text = "FormatDriveText".GetLocalizedResource(),
					Command = FormatDriveCommand,
					CommandParameter = item,
					ShowItem = options?.ShowFormatDrive ?? false
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
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}
			.Where(x => x.ShowItem).ToList();
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
					if (!DriveItems.Any(x => x.Item == drive) && drive.Type != DataModels.NavigationControlItems.DriveType.VirtualDrive)
					{
						var cardItem = new DriveCardItem(drive);
						DriveItems.AddSorted(cardItem);
						await cardItem.LoadCardThumbnailAsync(); // After add
					}
				}

				foreach (DriveCardItem driveCard in DriveItems.ToList())
				{
					if (!App.DrivesManager.Drives.Contains(driveCard.Item))
						DriveItems.Remove(driveCard);
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

		private void FormatDrive(DriveCardItem? item)
		{
			Win32API.OpenFormatDriveDialog(item?.Path ?? string.Empty);
		}

		private void OpenProperties(DriveCardItem item)
		{
			EventHandler<object> flyoutClosed = null!;

			flyoutClosed = async (s, e) =>
			{
				ItemContextMenuFlyout.Closed -= flyoutClosed;
				await FilePropertiesHelpers.OpenPropertiesWindowAsync(item.Item, _AppInstance);
			};

			ItemContextMenuFlyout.Closed += flyoutClosed;
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
			var updateTasks = DriveItems.Select(item => item.Item.UpdatePropertiesAsync());
			await Task.WhenAll(updateTasks);
		}

		public void Dispose()
		{
		}
		#endregion
	}
}
