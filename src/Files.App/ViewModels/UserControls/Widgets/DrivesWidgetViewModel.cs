// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="DrivesWidget"/>.
	/// </summary>
	public sealed partial class DrivesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Properties

		public ObservableCollection<WidgetDriveCardItem> Items { get; } = [];

		public string WidgetName => nameof(DrivesWidget);
		public string AutomationProperties => Strings.Drives.GetLocalizedResource();
		public string WidgetHeader => Strings.Drives.GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowDrivesWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		// Commands

		private ICommand EjectDeviceCommand { get; } = null!;

		// Constructor

		public DrivesWidgetViewModel()
		{
			Items.CollectionChanged += Items_CollectionChanged;
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

			PinToSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecutePinToSidebarCommand);
			UnpinFromSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteUnpinFromSidebarCommand);
			EjectDeviceCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteEjectDeviceCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteOpenPropertiesCommand);
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				foreach (var item in Items)
					item.Dispose();

				Items.Clear();

				await foreach (IWindowsFolder folder in HomePageContext.HomeFolder.GetLogicalDrivesAsync(default))
				{
					folder.TryGetDriveTotalSpace(out var totalSize);
					folder.TryGetDriveFreeSpace(out var freeSize);
					folder.TryGetDriveType(out var driveType);

					Items.Insert(
						Items.Count,
						new()
						{
							Item = folder,
							Text = folder.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI),
							Path = folder.GetDisplayName(SIGDN.SIGDN_FILESYSPATH),
							TotalSize = ByteSizeLib.ByteSize.FromBytes(totalSize),
							FreeSize = ByteSizeLib.ByteSize.FromBytes(freeSize),
							DriveType = (SystemIO.DriveType)driveType,
						});
				}
			});
		}

		public async Task NavigateToPath(string path)
		{
			if (await DriveHelpers.CheckEmptyDrive(path))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path);
				return;
			}

			if (ContentPageContext.ShellPage is not IShellPage shellPage)
				return;

			shellPage.NavigateWithArguments(
				shellPage.InstanceViewModel.FolderSettings.GetLayoutType(path),
				new() { NavPathParam = path });
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			if (item is not WidgetDriveCardItem driveItem)
				return [];

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewTabFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab && CommandManager.OpenInNewTabFromHomeAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewWindowFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow && CommandManager.OpenInNewWindowFromHomeAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewPaneFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane && CommandManager.OpenInNewPaneFromHomeAction.IsExecutable
				}.Build(),
				new()
				{
					Text = Strings.PinFolderToSidebar.GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel() { ThemedIconStyle = "App.ThemedIcons.FavoritePin" },
					Command = PinToSidebarCommand,
					CommandParameter = item,
					ShowItem = !isPinned
				},
				new()
				{
					Text = Strings.UnpinFolderFromSidebar.GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel() { ThemedIconStyle = "App.ThemedIcons.FavoritePinRemove" },
					Command = UnpinFromSidebarCommand,
					CommandParameter = item,
					ShowItem = isPinned
				},
				new()
				{
					Text = Strings.Eject.GetLocalizedResource(),
					Command = EjectDeviceCommand,
					CommandParameter = item,
					ShowItem = driveItem.DriveType is SystemIO.DriveType.Removable or SystemIO.DriveType.CDRom
				},
				new()
				{
					Text = Strings.Properties.GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel() { ThemedIconStyle = "App.ThemedIcons.Properties" },
					Command = OpenPropertiesCommand,
					CommandParameter = item
				},
				new()
				{
					Text = Strings.TurnOnBitLocker.GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					IsEnabled = false
				},
				new()
				{
					Text = Strings.ManageBitLocker.GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowItem = CommandManager.OpenTerminalFromHome.IsExecutable ||
						CommandManager.OpenStorageSenseFromHome.IsExecutable ||
						CommandManager.FormatDriveFromHome.IsExecutable
				},
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenTerminalFromHome).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenStorageSenseFromHome).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.FormatDriveFromHome).Build(),
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new()
				{
					Text = Strings.Loading.GetLocalizedResource(),
					Glyph = "\xE712",
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		// Command methods

		private void ExecuteEjectDeviceCommand(WidgetDriveCardItem? item)
		{
			if (item is null)
				return;

			DriveHelpers.EjectDeviceAsync(item.Path);
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

		// Event methods

		private async void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add)
			{
				foreach (WidgetDriveCardItem cardItem in e.NewItems!)
					await cardItem.LoadCardThumbnailAsync();
			}
		}

		private async void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			if (e.SettingName == nameof(UserSettingsService.FoldersSettingsService.SizeUnitFormat))
				await RefreshWidgetAsync();
		}

		// Disposer

		public void Dispose()
		{
			foreach (var item in Items)
				item.Dispose();
		}
	}
}
