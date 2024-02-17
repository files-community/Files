// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
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

		public ObservableCollection<WidgetDriveCardItem> Items { get; } = [];

		public string WidgetName => nameof(DrivesWidgetViewModel);
		public string AutomationProperties => "Drives".GetLocalizedResource();
		public string WidgetHeader => "Drives".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowDrivesWidget;
		public bool ShowMenuFlyout => true;
		public MenuFlyoutItem? MenuFlyoutItem { get; }

		// Commands

		public ICommand OpenMapNetworkDriveDialogCommand;

		// Constructor

		public DrivesWidgetViewModel()
		{
			_ = RefreshWidgetAsync();

			DrivesViewModel.Drives.CollectionChanged += async (s, e) => await RefreshWidgetAsync();

			MenuFlyoutItem = new()
			{
				Icon = new FontIcon() { Glyph = "\uE710" },
				Text = "DrivesWidgetOptionsFlyoutMapNetDriveMenuItem/Text".GetLocalizedResource(),
				Command = OpenMapNetworkDriveDialogCommand
			};

			OpenMapNetworkDriveDialogCommand = new AsyncRelayCommand(ExecuteOpenMapNetworkDriveDialogCommand);
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				try
				{

					// Add newly added items
					foreach (DriveItem drive in DrivesViewModel.Drives.ToList().Cast<DriveItem>())
					{
						if (!Items.Any(x => x.Item == drive) && drive.Type != DriveType.VirtualDrive)
						{
							// Add item
							var cardItem = new WidgetDriveCardItem(drive);
							Items.AddSorted(cardItem);

							await cardItem.LoadCardThumbnailAsync();
						}
					}

					// Upload properties information
					var updateTasks = Items.Select(item => item.Item.UpdatePropertiesAsync());
					await Task.WhenAll(updateTasks);
				}
				catch (Exception ex)
				{
					App.Logger.LogInformation(ex, "Could not populate recent files");
				}
			});
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

		// Command methods

		private Task ExecuteOpenMapNetworkDriveDialogCommand()
		{
			return NetworkDrivesViewModel.OpenMapNetworkDriveDialogAsync();
		}

		// Disposer

		public void Dispose()
		{
		}
	}
}
