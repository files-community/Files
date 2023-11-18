// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class DrivesWidget : HomePageWidget, IWidgetItemModel, INotifyPropertyChanged
	{
		// Dependency injections

		private NetworkDrivesViewModel NetworkDrivesViewModel { get; } = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();

		// Events

		public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;
		public event DrivesWidgetNewPaneInvokedEventHandler DrivesWidgetNewPaneInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Properties

		public static ObservableCollection<DriveCardItem> ItemsAdded { get; } = new();

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

		public string WidgetName => nameof(DrivesWidget);
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

		public ICommand MapNetworkDriveCommand { get; }

		// Constructor

		public DrivesWidget()
		{
			InitializeComponent();

			Drives_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			DrivesViewModel.Drives.CollectionChanged += Drives_CollectionChanged;
		}

		// Event methods

		private async void Drives_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				foreach (DriveItem drive in DrivesViewModel.Drives.ToList().Cast<DriveItem>())
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
			var drive = ItemsAdded.Where(x =>
				string.Equals(
					PathNormalization.NormalizePath(x.Path),
					PathNormalization.NormalizePath(item.Path),
					StringComparison.OrdinalIgnoreCase))
				.FirstOrDefault();

			if (drive is null)
				return default;

			var items = SideBarDriveItemContextMenuFactory.Generate(drive, item, isPinned, isFolder);

			return items;
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

		private void MenuFlyout_Opening(object sender, object e)
		{
			var pinToFavoritesItem = (sender as MenuFlyout).Items.Single(x => x.Name == "PinToFavorites");
			pinToFavoritesItem.Visibility = (pinToFavoritesItem.DataContext as DriveItem).IsPinned ? Visibility.Collapsed : Visibility.Visible;

			var unpinFromFavoritesItem = (sender as MenuFlyout).Items.Single(x => x.Name == "UnpinFromFavorites");
			unpinFromFavoritesItem.Visibility = (unpinFromFavoritesItem.DataContext as DriveItem).IsPinned ? Visibility.Visible : Visibility.Collapsed;
		}

		private void GoToStorageSense_Click(object sender, RoutedEventArgs e)
		{
			string clickedCard = (sender as Button).Tag.ToString();
			StorageSenseHelper.OpenStorageSenseAsync(clickedCard);
		}

		public async Task RefreshWidgetAsync()
		{
			var updateTasks = ItemsAdded.Select(item => item.Item.UpdatePropertiesAsync());
			await Task.WhenAll(updateTasks);
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Dispose()
		{
		}
	}
}
