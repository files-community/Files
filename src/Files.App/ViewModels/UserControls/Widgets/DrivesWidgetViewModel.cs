// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
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

		public ObservableCollection<WidgetDriveCardItem> Items { get; } = new();

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
			set => SetProperty(ref _AppInstance, value);
		}

		// Events

		public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public event DrivesWidgetNewPaneInvokedEventHandler? DrivesWidgetNewPaneInvoked;
		public event DrivesWidgetInvokedEventHandler? DrivesWidgetInvoked;

		// Commands

		private ICommand FormatDriveCommand { get; set; }
		private ICommand EjectDeviceCommand { get; set; }
		private ICommand DisconnectNetworkDriveCommand { get; set; }
		private ICommand OpenInNewPaneCommand { get; set; }
		private ICommand MapNetworkDriveCommand { get; set; }

		// Constructor

		public DrivesWidgetViewModel()
		{
			Drives_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			DrivesViewModel.Drives.CollectionChanged += Drives_CollectionChanged;

			MapNetworkDriveCommand = new AsyncRelayCommand(ExecuteMapNetworkDriveCommand);
			EjectDeviceCommand = new AsyncRelayCommand<WidgetDriveCardItem>(ExecuteEjectDeviceCommand);
			FormatDriveCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteFormatDriveCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteOpenPropertiesCommand);
			DisconnectNetworkDriveCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteDisconnectNetworkDriveCommand);
			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewTabCommand);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewWindowCommand);
			OpenInNewPaneCommand = new AsyncRelayCommand<WidgetDriveCardItem>(ExecuteOpenInNewPaneCommand);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(ExecutePinToFavoritesCommand);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteUnpinFromFavoritesCommand);
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			var updateTasks = Items.Select(item => item.Item.UpdatePropertiesAsync());
			await Task.WhenAll(updateTasks);
		}

		public async Task GoToItem(object sender)
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

		protected override List<ContextMenuFlyoutItemViewModel> GenerateRightClickContextMenu(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			var drive = Items.Where(x => string.Equals(PathNormalization.NormalizePath(x.Path ?? string.Empty), PathNormalization.NormalizePath(item.Path ?? string.Empty), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
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

		private async void Drives_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				foreach (DriveItem drive in DrivesViewModel.Drives.ToList().Cast<DriveItem>())
				{
					if (!Items.Any(x => x.Item == drive) && drive.Type != DriveType.VirtualDrive)
					{
						var cardItem = new WidgetDriveCardItem(drive);
						Items.AddSorted(cardItem);
						await cardItem.LoadCardThumbnailAsync(); // After add
					}
				}

				foreach (WidgetDriveCardItem driveCard in Items.ToList())
				{
					if (!DrivesViewModel.Drives.Contains(driveCard.Item))
						Items.Remove(driveCard);
				}
			});
		}

		// Command methods

		private void ExecuteFormatDriveCommand(WidgetDriveCardItem? item)
		{
			Win32API.OpenFormatDriveDialog(item?.Path ?? string.Empty);
		}

		private void ExecuteOpenPropertiesCommand(WidgetDriveCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked)
				return;

			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				HomePageContext.ItemContextFlyoutMenu!.Closed -= flyoutClosed;
				FilePropertiesHelpers.OpenPropertiesWindow(item!.Item, AppInstance!);
			};

			HomePageContext.ItemContextFlyoutMenu!.Closed += flyoutClosed;
		}

		private void ExecuteDisconnectNetworkDriveCommand(WidgetDriveCardItem? item)
		{
			NetworkDrivesViewModel.DisconnectNetworkDrive(item!.Item);
		}

		private Task ExecuteMapNetworkDriveCommand()
		{
			return NetworkDrivesViewModel.OpenMapNetworkDriveDialogAsync();
		}

		private async Task ExecuteEjectDeviceCommand(WidgetDriveCardItem? item)
		{
			var result = await DriveHelpers.EjectDeviceAsync(item!.Item.Path);

			await UIHelpers.ShowDeviceEjectResultAsync(item.Item.Type, result);
		}

		private async Task ExecuteOpenInNewPaneCommand(WidgetDriveCardItem? item)
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
