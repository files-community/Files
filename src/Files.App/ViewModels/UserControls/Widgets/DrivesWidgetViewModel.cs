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

			return WidgetDriveCardItemContextFlyoutFactory.Generate();
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

		// Disposer

		public void Dispose()
		{
		}
	}
}
