﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="NetworkLocationsWidget"/>.
	/// </summary>
	public sealed class NetworkLocationsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Properties

		public ObservableCollection<WidgetDriveCardItem> Items { get; } = [];

		public string WidgetName => nameof(NetworkLocationsWidget);
		public string AutomationProperties => "NetworkLocations".GetLocalizedResource();
		public string WidgetHeader => "NetworkLocations".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget;
		public bool ShowMenuFlyout => true;
		public MenuFlyoutItem? MenuFlyoutItem => new()
		{
			Icon = new FontIcon() { Glyph = "\uE710" },
			Text = "DrivesWidgetOptionsFlyoutMapNetDriveMenuItem/Text".GetLocalizedResource(),
			Command = MapNetworkDriveCommand
		};

		private bool _IsNoNetworkLocations;
		public bool IsNoNetworkLocations
		{
			get => _IsNoNetworkLocations;
			private set => SetProperty(ref _IsNoNetworkLocations, value);
		}

		// Commands

		private ICommand FormatDriveCommand { get; } = null!;
		private ICommand EjectDeviceCommand { get; } = null!;
		private ICommand OpenInNewPaneCommand { get; } = null!;
		private ICommand MapNetworkDriveCommand { get; } = null!;
		private ICommand DisconnectNetworkDriveCommand { get; } = null!;

		// Constructor

		public NetworkLocationsWidgetViewModel()
		{
			Drives_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Shortcuts_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			DrivesViewModel.Drives.CollectionChanged += Drives_CollectionChanged;
			NetworkService.Shortcuts.CollectionChanged += Shortcuts_CollectionChanged;

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewTabCommand);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewWindowCommand);
			PinToSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecutePinToSidebarCommand);
			UnpinFromSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteUnpinFromSidebarCommand);
			FormatDriveCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteFormatDriveCommand);
			EjectDeviceCommand = new AsyncRelayCommand<WidgetDriveCardItem>(ExecuteEjectDeviceCommand);
			OpenInNewPaneCommand = new AsyncRelayCommand<WidgetDriveCardItem>(ExecuteOpenInNewPaneCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteOpenPropertiesCommand);
			DisconnectNetworkDriveCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteDisconnectNetworkDriveCommand);
			MapNetworkDriveCommand = new AsyncRelayCommand(ExecuteMapNetworkDriveCommand);
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			var updateTasks = Items.Select(item => item.Item.UpdatePropertiesAsync());
			await Task.WhenAll(updateTasks);
		}

		public async Task NavigateToPath(string path)
		{
			if (await DriveHelpers.CheckEmptyDrive(path))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path, false);
				return;
			}

			ContentPageContext.ShellPage!.NavigateWithArguments(
				ContentPageContext.ShellPage!.InstanceViewModel.FolderSettings.GetLayoutType(path),
				new() { NavPathParam = path });
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			var drive =
				Items.Where(x =>
					string.Equals(
						PathNormalization.NormalizePath(x.Path!),
						PathNormalization.NormalizePath(item.Path!),
					StringComparison.OrdinalIgnoreCase))
				.FirstOrDefault();

			var options = drive?.Item.MenuOptions;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel() { OpacityIconStyle = "ColorIconOpenInNewTab" },
					Command = OpenInNewTabCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel() { OpacityIconStyle = "ColorIconOpenInNewWindow" },
					Command = OpenInNewWindowCommand,
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
					Text = "PinFolderToSidebar".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel() { OpacityIconStyle = "Icons.Pin.16x16" },
					Command = PinToSidebarCommand,
					CommandParameter = item,
					ShowItem = !isPinned
				},
				new()
				{
					Text = "UnpinFolderFromSidebar".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel() { OpacityIconStyle = "Icons.Unpin.16x16" },
					Command = UnpinFromSidebarCommand,
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
					OpacityIcon = new OpacityIconModel() { OpacityIconStyle = "ColorIconProperties" },
					Command = OpenPropertiesCommand,
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

		public void DisableWidget()
		{
			UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget = false;
		}

		// Command methods

		private async Task ExecuteEjectDeviceCommand(WidgetDriveCardItem? item)
		{
			if (item is null)
				return;

			var result = await DriveHelpers.EjectDeviceAsync(item.Item.Path);
			await UIHelpers.ShowDeviceEjectResultAsync(item.Item.Type, result);
		}

		private async Task ExecuteOpenInNewPaneCommand(WidgetDriveCardItem? item)
		{
			if (item is null || await DriveHelpers.CheckEmptyDrive(item.Item.Path))
				return;

			ContentPageContext.ShellPage!.PaneHolder?.OpenSecondaryPane(item.Item.Path);
		}

		private Task ExecuteMapNetworkDriveCommand()
		{
			return NetworkService.OpenMapNetworkDriveDialogAsync();
		}

		private void ExecuteFormatDriveCommand(WidgetDriveCardItem? item)
		{
			Win32Helper.OpenFormatDriveDialog(item?.Path ?? string.Empty);
		}

		private void ExecuteOpenPropertiesCommand(WidgetDriveCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked || item is null)
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

		private void ExecuteDisconnectNetworkDriveCommand(WidgetDriveCardItem? item)
		{
			if (item is null)
				return;

			NetworkService.DisconnectNetworkDrive(item.Item);
		}

		private async Task UpdateItems(ObservableCollection<ILocatableFolder> source)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				IsNoNetworkLocations = false;

				foreach (DriveItem drive in source.ToList().Cast<DriveItem>())
				{
					if (!Items.Any(x => x.Item == drive) && drive.Type is DriveType.Network)
					{
						var cardItem = new WidgetDriveCardItem(drive);
						Items.AddSorted(cardItem);

						await cardItem.LoadCardThumbnailAsync();
					}
				}

				foreach (WidgetDriveCardItem driveCard in Items.ToList())
				{
					if (!DrivesViewModel.Drives.Contains(driveCard.Item) && !NetworkService.Shortcuts.Contains(driveCard.Item))
						Items.Remove(driveCard);
				}

				IsNoNetworkLocations = !Items.Any();
			});
		}	

		// Event methods

		private async void Drives_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await UpdateItems(DrivesViewModel.Drives);
		}

		private async void Shortcuts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await UpdateItems(NetworkService.Shortcuts);
		}

		// Disposer

		public void Dispose()
		{
		}
	}
}
