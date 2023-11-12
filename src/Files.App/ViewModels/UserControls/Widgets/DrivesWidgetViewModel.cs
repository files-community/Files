// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class DrivesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged
	{
		// Dependency injections

		private NetworkDrivesViewModel NetworkDrivesViewModel { get; } = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();

		// Widget information

		public string WidgetName => "Drives";
		public string AutomationProperties => "DrivesWidgetAutomationProperties/Name".GetLocalizedResource();
		public string WidgetHeader => "Drives".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => base.UserSettingsService.GeneralSettingsService.ShowDrivesWidget;
		public bool ShowMenuFlyout => true;

		// Fields & properties
		public ObservableCollection<DriveCardItem> ItemsAdded = new();

		private IShellPage associatedInstance;
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

		public MenuFlyoutItem MenuFlyoutItem => new()
		{
			Icon = new FontIcon() { Glyph = "\uE710" },
			Text = "DrivesWidgetOptionsFlyoutMapNetDriveMenuItem/Text".GetLocalizedResource(),
			Command = MapNetworkDriveCommand
		};

		// Events

		public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;
		public event DrivesWidgetNewPaneInvokedEventHandler DrivesWidgetNewPaneInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Commands

		public ICommand FormatDriveCommand;
		public ICommand EjectDeviceCommand;
		public ICommand DisconnectNetworkDriveCommand;
		public ICommand GoToStorageSenseCommand;
		public ICommand OpenInNewPaneCommand;
		public ICommand MapNetworkDriveCommand;

		public DrivesWidgetViewModel()
		{
			Drives_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			DrivesViewModel.Drives.CollectionChanged += Drives_CollectionChanged;

			FormatDriveCommand = new RelayCommand<DriveCardItem>(FormatDrive);
			EjectDeviceCommand = new AsyncRelayCommand<DriveCardItem>(EjectDeviceAsync);
			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewTabAsync);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewWindowAsync);
			OpenInNewPaneCommand = new AsyncRelayCommand<DriveCardItem>(OpenInNewPaneAsync);
			OpenPropertiesCommand = new RelayCommand<DriveCardItem>(OpenProperties);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(PinToFavoritesAsync);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(UnpinFromFavoritesAsync);
			MapNetworkDriveCommand = new AsyncRelayCommand(DoNetworkMapDriveAsync);
			DisconnectNetworkDriveCommand = new RelayCommand<DriveCardItem>(DisconnectNetworkDrive);
		}

		public async Task Open(string path)
		{
			if (await DriveHelpers.CheckEmptyDrive(path))
				return;

			var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path);

				return;
			}

			DrivesWidgetInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
			{
				Path = path
			});
		}

		private async void Drives_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				foreach (DriveItem drive in DrivesViewModel.Drives.ToList())
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
					if (!DrivesViewModel.Drives.Contains(driveCard.Item))
						ItemsAdded.Remove(driveCard);
				}
			});
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			var drive = ItemsAdded.Where(x => string.Equals(PathNormalization.NormalizePath(x.Path), PathNormalization.NormalizePath(item.Path), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
			var options = drive?.Item.MenuOptions;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab
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
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane
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
					ShowItem = !isPinned
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
					ShowItem = isPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Eject".GetLocalizedResource(),
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
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
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

		private Task DoNetworkMapDriveAsync()
		{
			return NetworkDrivesViewModel.OpenMapNetworkDriveDialogAsync();
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private async Task EjectDeviceAsync(DriveCardItem item)
		{
			var result = await DriveHelpers.EjectDeviceAsync(item.Item.Path);
			await UIHelpers.ShowDeviceEjectResultAsync(item.Item.Type, result);
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
				FilePropertiesHelpers.OpenPropertiesWindow(item.Item, associatedInstance);
			};
			ItemContextMenuFlyout.Closed += flyoutClosed;
		}

		private async Task OpenInNewPaneAsync(DriveCardItem item)
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
			NetworkDrivesViewModel.DisconnectNetworkDrive(item.Item);
		}

		public async Task RefreshWidgetAsync()
		{
			var updateTasks = ItemsAdded.Select(item => item.Item.UpdatePropertiesAsync());
			await Task.WhenAll(updateTasks);
		}

		public void Dispose()
		{

		}
	}
}
