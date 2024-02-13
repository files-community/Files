// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class DrivesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Dependency injections

		private NetworkDrivesViewModel NetworkDrivesViewModel { get; } = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();

		// Properties

		public ObservableCollection<WidgetDriveCardItem> Items = [];

		public string WidgetName => nameof(DrivesWidgetViewModel);
		public string AutomationProperties => "DrivesWidgetAutomationProperties/Name".GetLocalizedResource();
		public string WidgetHeader => "Drives".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowDrivesWidget;
		public bool ShowMenuFlyout => true;
		public MenuFlyoutItem? MenuFlyoutItem { get; }

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

			MenuFlyoutItem = new()
			{
				Icon = new FontIcon() { Glyph = "\uE710" },
				Text = "DrivesWidgetOptionsFlyoutMapNetDriveMenuItem/Text".GetLocalizedResource(),
				Command = MapNetworkDriveCommand
			};

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewTabAsync);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewWindowAsync);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(PinToFavoritesAsync);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(UnpinFromFavoritesAsync);
			OpenPropertiesCommand = new RelayCommand<WidgetDriveCardItem>(OpenProperties);
			FormatDriveCommand = new RelayCommand<WidgetDriveCardItem>(FormatDrive);
			EjectDeviceCommand = new AsyncRelayCommand<WidgetDriveCardItem>(EjectDeviceAsync);
			OpenInNewPaneCommand = new AsyncRelayCommand<WidgetDriveCardItem>(OpenInNewPaneAsync);
			MapNetworkDriveCommand = new AsyncRelayCommand(DoNetworkMapDriveAsync);
			DisconnectNetworkDriveCommand = new RelayCommand<WidgetDriveCardItem>(DisconnectNetworkDrive);
		}

		// Methods

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			var drive =
				Items.Where(x =>
					string.Equals(
						PathNormalization.NormalizePath(x.Path ?? string.Empty),
						PathNormalization.NormalizePath(item.Path ?? string.Empty),
						StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

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
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		public async Task RefreshWidgetAsync()
		{
			var updateTasks = Items.Select(item => item.Item.UpdatePropertiesAsync());
			await Task.WhenAll(updateTasks);
		}

		public async Task OpenFileLocation(string path)
		{
			if (await DriveHelpers.CheckEmptyDrive(path))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path);
				return;
			}

			ContentPageContext.ShellPage!.NavigateWithArguments(
				ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings.GetLayoutType(path)!,
				new() { NavPathParam = path });
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

						await cardItem.LoadCardThumbnailAsync();
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

		private void FormatDrive(WidgetDriveCardItem? item)
		{
			Win32API.OpenFormatDriveDialog(item?.Path ?? string.Empty);
		}

		private void OpenProperties(WidgetDriveCardItem? item)
		{
			if (item is null || !HomePageContext.IsAnyItemRightClicked)
				return;

			var flyout = HomePageContext.ItemContextFlyoutMenu;

			EventHandler<object> flyoutClosed = null!;

			flyoutClosed = (s, e) =>
			{
				flyout!.Closed -= flyoutClosed;
				FilePropertiesHelpers.OpenPropertiesWindow(item.Item, ContentPageContext.ShellPage!);
			};

			flyout!.Closed += flyoutClosed;
		}

		private void DisconnectNetworkDrive(WidgetDriveCardItem? item)
		{
			if (item is null)
				return;

			NetworkDrivesViewModel.DisconnectNetworkDrive(item.Item);
		}

		private Task DoNetworkMapDriveAsync()
		{
			return NetworkDrivesViewModel.OpenMapNetworkDriveDialogAsync();
		}

		private async Task OpenInNewPaneAsync(WidgetDriveCardItem? item)
		{
			if (item is null)
				return;

			if (await DriveHelpers.CheckEmptyDrive(item.Item.Path))
				return;

			ContentPageContext.ShellPage!.PaneHolder?.OpenPathInNewPane(item.Item.Path);
		}

		private async Task EjectDeviceAsync(WidgetDriveCardItem? item)
		{
			if (item is null)
				return;

			var result = await DriveHelpers.EjectDeviceAsync(item.Item.Path);
			await UIHelpers.ShowDeviceEjectResultAsync(item.Item.Type, result);
		}

		public void Dispose()
		{
		}
	}
}
