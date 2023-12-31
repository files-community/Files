// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents ViewModel for <see cref="DrivesWidget"/>.
	/// </summary>
	public class DrivesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged
	{
		// Properties

		public ObservableCollection<DriveCardItem> Items = new();

		public string WidgetName => nameof(DrivesWidgetViewModel);
		public string AutomationProperties => "DrivesWidgetAutomationProperties/Name".GetLocalizedResource();
		public string WidgetHeader => "Drives".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowDrivesWidget;
		public bool ShowMenuFlyout => true;
		public MenuFlyoutItem MenuFlyoutItem => new()
		{
			Icon = new FontIcon() { Glyph = "\uE710" },
			Text = "DrivesWidgetOptionsFlyoutMapNetDriveMenuItem/Text".GetLocalizedResource(),
			Command = MapNetworkDriveCommand
		};

		private IShellPage? _AppInstance;
		public IShellPage? AppInstance
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

		// Events

		public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public event DrivesWidgetInvokedEventHandler? DrivesWidgetInvoked;
		public event DrivesWidgetNewPaneInvokedEventHandler? DrivesWidgetNewPaneInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Commands

		public ICommand FormatDriveCommand;
		public ICommand EjectDeviceCommand;
		public ICommand DisconnectNetworkDriveCommand;
		public ICommand OpenInNewPaneCommand;
		public ICommand MapNetworkDriveCommand;

		// Constructor

		public DrivesWidgetViewModel()
		{
			Drives_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			DrivesViewModel.Drives.CollectionChanged += Drives_CollectionChanged;

			MapNetworkDriveCommand = new AsyncRelayCommand(ExecuteMapNetworkDriveCommand);
			EjectDeviceCommand = new AsyncRelayCommand<DriveCardItem>(ExecuteEjectDeviceCommand);
			FormatDriveCommand = new RelayCommand<DriveCardItem>(ExecuteFormatDriveCommand);
			OpenPropertiesCommand = new RelayCommand<DriveCardItem>(ExecuteOpenPropertiesCommand);
			DisconnectNetworkDriveCommand = new RelayCommand<DriveCardItem>(ExecuteDisconnectNetworkDriveCommand);

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewTabCommand);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewWindowCommand);
			OpenInNewPaneCommand = new AsyncRelayCommand<DriveCardItem>(ExecuteOpenInNewPaneCommand);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(ExecutePinToFavoritesCommand);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteUnpinFromFavoritesCommand);
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			var updateTasks = Items.Select(item => item.Item.UpdatePropertiesAsync());
			await Task.WhenAll(updateTasks);
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			var drive = Items.Where(x => string.Equals(PathNormalization.NormalizePath(x.Path), PathNormalization.NormalizePath(item.Path), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
			var options = drive?.Item.MenuOptions;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand!,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand!,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow
				},
				new()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane
				},
				new()
				{
					Text = "PinToFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconPinToFavorites",
					},
					Command = PinToFavoritesCommand!,
					CommandParameter = item,
					ShowItem = !isPinned
				},
				new()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconUnpinFromFavorites",
					},
					Command = UnpinFromFavoritesCommand!,
					CommandParameter = item,
					ShowItem = isPinned
				},
				new()
				{
					Text = "Eject".GetLocalizedResource(),
					Command = EjectDeviceCommand,
					CommandParameter = item,
					ShowItem = options?.ShowEjectDevice ?? false
				},
				new()
				{
					Text = "FormatDriveText".GetLocalizedResource(),
					Command = FormatDriveCommand,
					CommandParameter = item,
					ShowItem = options?.ShowFormatDrive ?? false
				},
				new()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand!,
					CommandParameter = item
				},
				new()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					IsEnabled = false
				},
				new()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					IsEnabled = false
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new()
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

		// Event methods

		private void MenuFlyout_Opening(object sender, object e)
		{
			var pinToFavoritesItem = (sender as MenuFlyout)!.Items.Single(x => x.Name == "PinToFavorites");
			pinToFavoritesItem.Visibility = (pinToFavoritesItem.DataContext as DriveItem)!.IsPinned ? Visibility.Collapsed : Visibility.Visible;

			var unpinFromFavoritesItem = (sender as MenuFlyout)!.Items.Single(x => x.Name == "UnpinFromFavorites");
			unpinFromFavoritesItem.Visibility = (unpinFromFavoritesItem.DataContext as DriveItem)!.IsPinned ? Visibility.Visible : Visibility.Collapsed;
		}

		private void GoToStorageSense_Click(object sender, RoutedEventArgs e)
		{
			string clickedCard = (sender as Button).Tag.ToString();
			StorageSenseHelper.OpenStorageSenseAsync(clickedCard);
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button)
				return;

			string pathToNavigate = button.Tag.ToString()!;

			if (await DriveHelpers.CheckEmptyDrive(pathToNavigate))
				return;

			var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(pathToNavigate);

				return;
			}

			DrivesWidgetInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
			{
				Path = pathToNavigate
			});
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed ||
				sender is not Button button)
				return;

			string navigationPath = button.Tag.ToString()!;

			if (await DriveHelpers.CheckEmptyDrive(navigationPath))
				return;

			await NavigationHelpers.OpenPathInNewTab(navigationPath);
		}

		private async void Drives_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				foreach (DriveItem drive in DrivesViewModel.Drives.ToList().Cast<DriveItem>())
				{
					if (!Items.Any(x => x.Item == drive) && drive.Type != DriveType.VirtualDrive)
					{
						var cardItem = new DriveCardItem(drive);
						Items.AddSorted(cardItem);
						await cardItem.LoadCardThumbnailAsync(); // After add
					}
				}

				foreach (DriveCardItem driveCard in Items.ToList())
				{
					if (!DrivesViewModel.Drives.Contains(driveCard.Item))
						Items.Remove(driveCard);
				}
			});
		}

		// Command methods

		private void ExecuteFormatDriveCommand(DriveCardItem? item)
		{
			Win32API.OpenFormatDriveDialog(item?.Path ?? string.Empty);
		}

		private void ExecuteOpenPropertiesCommand(DriveCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked)
				return;

			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				HomePageContext.ItemContextFlyoutMenu!.Closed -= flyoutClosed;
				FilePropertiesHelpers.OpenPropertiesWindow(item!.Item, _AppInstance!);
			};

			HomePageContext.ItemContextFlyoutMenu!.Closed += flyoutClosed;
		}

		private void ExecuteDisconnectNetworkDriveCommand(DriveCardItem? item)
		{
			NetworkDrivesViewModel.DisconnectNetworkDrive(item!.Item);
		}

		private Task ExecuteMapNetworkDriveCommand()
		{
			return NetworkDrivesViewModel.OpenMapNetworkDriveDialogAsync();
		}

		private async Task ExecuteEjectDeviceCommand(DriveCardItem? item)
		{
			var result = await DriveHelpers.EjectDeviceAsync(item!.Item.Path);

			await UIHelpers.ShowDeviceEjectResultAsync(item.Item.Type, result);
		}

		private async Task ExecuteOpenInNewPaneCommand(DriveCardItem? item)
		{
			if (await DriveHelpers.CheckEmptyDrive(item!.Item.Path))
				return;

			DrivesWidgetNewPaneInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs() { Path = item.Item.Path });
		}

		// Disposer

		public void Dispose()
		{
		}
	}
}
