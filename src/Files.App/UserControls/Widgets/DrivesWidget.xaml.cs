// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents group of control displays a list of <see cref="WidgetDriveCardItem"/>.
	/// </summary>
	public sealed partial class DrivesWidget : UserControl
	{
<<<<<<< HEAD
<<<<<<< HEAD
		private DrivesWidgetViewModel ViewModel { get; set; }

		public IUserSettingsService userSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		private DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private NetworkDrivesViewModel networkDrivesViewModel = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();

		public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;
		public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);
		public event DrivesWidgetNewPaneInvokedEventHandler DrivesWidgetNewPaneInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;
		public static ObservableCollection<WidgetDriveCardItem> ItemsAdded = new();

		private IShellPage associatedInstance;

		public ICommand FormatDriveCommand;
		public ICommand EjectDeviceCommand;
		public ICommand DisconnectNetworkDriveCommand;
		public ICommand GoToStorageSenseCommand;
		public ICommand OpenInNewPaneCommand;

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
		public MenuFlyoutItem MenuFlyoutItem => new MenuFlyoutItem()
		{
			Icon = new FontIcon() { Glyph = "\uE710" },
			Text = "DrivesWidgetOptionsFlyoutMapNetDriveMenuItem/Text".GetLocalizedResource(),
			Command = MapNetworkDriveCommand
		};

		public AsyncRelayCommand MapNetworkDriveCommand { get; }
=======
		private DrivesWidgetViewModel ViewModel { get; set; } = new();
>>>>>>> 70e2ff662 (Initial  commit)
=======
		public DrivesWidgetViewModel ViewModel { get; set; } = new();
>>>>>>> 145267b14 (Fix)

		public DrivesWidget()
		{
			InitializeComponent();
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button)
				return;

			var path = button.Tag.ToString() ?? string.Empty;

			await ViewModel.NavigateToPath(path);
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed || sender is not Button button)
				return;

			var path = button.Tag.ToString() ?? string.Empty;

			if (await DriveHelpers.CheckEmptyDrive(path))
				return;

			await NavigationHelpers.OpenPathInNewTab(path, false);
		}

		private void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.BuildItemContextMenu(sender, e);
		}

		private async void GoToStorageSense_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button)
				return;

			string path = button.Tag.ToString() ?? string.Empty;
			await StorageSenseHelper.OpenStorageSenseAsync(path);
		}
	}
}
