// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Windows.Input;
using Windows.System;

namespace Files.App.ViewModels
{
	/// <summary>
	/// Represents the ViewModel for <see cref="MainPage"/>.
	/// </summary>
	public class MainPageViewModel : ObservableObject
	{
		private readonly IUserSettingsService _userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IAppearanceSettingsService _appearanceSettingsService = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
		private readonly DrivesViewModel _drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private readonly NetworkDrivesViewModel _networkDrivesViewModel = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();
		private readonly IResourcesService _resourcesService = Ioc.Default.GetRequiredService<IResourcesService>();
		private SidebarViewModel SidebarAdaptiveViewModel { get; } = Ioc.Default.GetRequiredService<SidebarViewModel>();

		/// <summary>
		/// Gets the tab items of the current instance.
		/// </summary>
		public static ObservableCollection<TabBarItem> CurrentInstanceTabBarItems { get; } = new();

		/// <summary>
		/// Gets the TabBar control of the current instance.
		/// </summary>
		public ITabBar? CurrentInstanceTabBar { get; set; }

		// NOTE: This is useless because multi windowing is not supported for now
		public List<ITabBar> AllInstanceTabBars { get; private set; } = new();

		public double? ContentAreaActualWidth { get; set; }

		public bool ShouldViewControlBeDisplayed
			=> SidebarAdaptiveViewModel.PaneHolder?.ActivePane?.InstanceViewModel?.IsPageTypeNotHome ?? false;

		public bool ShouldPreviewPaneBeActive
			=> _userSettingsService.PreviewPaneSettingsService.IsEnabled && ShouldPreviewPaneBeDisplayed;

		public bool ShouldPreviewPaneBeDisplayed
		{
			get
			{
				var isHomePage = !(SidebarAdaptiveViewModel.PaneHolder?.ActivePane?.InstanceViewModel?.IsPageTypeNotHome ?? false);

				var isMultiPane = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneActive ?? false;

				var isBigEnough = MainWindow.Instance.Bounds.Width > 450 && MainWindow.Instance.Bounds.Height > 450 ||
					ContentAreaActualWidth > 700 && MainWindow.Instance.Bounds.Height > 360;

				var isEnabled = (!isHomePage || isMultiPane) && isBigEnough;

				return isEnabled;
			}
		}

		private TabBarItem? _SelectedTabBarItem;
		public TabBarItem? SelectedTabBarItem
		{
			get => _SelectedTabBarItem;
			set => SetProperty(ref _SelectedTabBarItem, value);
		}

		public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand;
		public ICommand OpenNewWindowAcceleratorCommand;

		public MainPageViewModel()
		{
			// Create commands
			NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(NavigateToNumberedTabKeyboardAccelerator);
			OpenNewWindowAcceleratorCommand = new AsyncRelayCommand<KeyboardAcceleratorInvokedEventArgs>(OpenNewWindowAcceleratorAsync);
		}

		public async Task OnNavigatedTo(NavigationEventArgs e)
		{
			// Initialize the static theme helper to capture a reference to this window
			// to handle theme changes without restarting the app
			var isThemeInitialized = ThemeHelper.Initialize();

			var parameter = e.Parameter;
			var ignoreStartupSettings = false;
			if (parameter is MainPageNavigationArguments mainPageNavigationArguments)
			{
				parameter = mainPageNavigationArguments.Parameter;
				ignoreStartupSettings = mainPageNavigationArguments.IgnoreStartupSettings;
			}

			// The navigation parameter is empty
			if (parameter is null || (parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
			{
				try
				{
					// Add last session tabs to closed tabs stack if those tabs are not about to be opened
					if (!_userSettingsService.AppSettingsService.RestoreTabsOnStartup &&
						!_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						_userSettingsService.GeneralSettingsService.LastSessionTabList != null)
					{
						var items = new CustomTabViewItemParameter[_userSettingsService.GeneralSettingsService.LastSessionTabList.Count];

						// Get parameters of the last session tabs
						for (int i = 0; i < items.Length; i++)
							items[i] = CustomTabViewItemParameter.Deserialize(_userSettingsService.GeneralSettingsService.LastSessionTabList[i]);

						// Restore recent tabs
						BaseTabBar.PushRecentTab(items);
					}

					// Restore the tabs
					if (_userSettingsService.AppSettingsService.RestoreTabsOnStartup)
					{
						_userSettingsService.AppSettingsService.RestoreTabsOnStartup = false;

						if (_userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in _userSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);

								await MultitaskingTabsHelpers.AddNewTabWithParameterAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}

							if (!_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
								_userSettingsService.GeneralSettingsService.LastSessionTabList = null;
						}
					}
					// Open specific path(s) stored in the list that can be modified from the Settings page
					else if (_userSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
						_userSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
					{
						foreach (string path in _userSettingsService.GeneralSettingsService.TabsOnStartupList)
							await MultitaskingTabsHelpers.AddNewTabWithPathAsync(typeof(PaneHolderPage), path);
					}
					// Continue with last session tabs
					else if (_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						_userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						foreach (string tabArgsString in _userSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
							await MultitaskingTabsHelpers.AddNewTabWithParameterAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
						}

						var defaultArg = new CustomTabViewItemParameter()
						{
							InitialPageType = typeof(PaneHolderPage),
							NavigationParameter = "Home"
						};

						_userSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
					}
					else
					{
						// Just add default page - Home
						await MultitaskingTabsHelpers.AddNewTabAsync();
					}
				}
				catch
				{
					// Just add default page - Home
					await MultitaskingTabsHelpers.AddNewTabAsync();
				}
			}
			else
			{
				if (!ignoreStartupSettings)
				{
					try
					{
						// Open specific path(s) stored in the list that can be modified from the Settings page
						if (_userSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
								_userSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
						{
							foreach (string path in _userSettingsService.GeneralSettingsService.TabsOnStartupList)
								await MultitaskingTabsHelpers.AddNewTabWithPathAsync(typeof(PaneHolderPage), path);
						}
						// Continue with last session tabs
						else if (_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
							_userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in _userSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);

								await MultitaskingTabsHelpers.AddNewTabWithParameterAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}

							var defaultArg = new CustomTabViewItemParameter()
							{
								InitialPageType = typeof(PaneHolderPage),
								NavigationParameter = "Home"
							};

							// Change the list to have the one item that indicates Home page
							_userSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
						}
					}
					catch
					{
					}
				}

				// The navigation parameter is string
				if (parameter is string navArgs)
					await MultitaskingTabsHelpers.AddNewTabWithPathAsync(typeof(PaneHolderPage), navArgs);
				// The navigation parameter is for the pane folder page
				else if (parameter is PaneNavigationArguments paneArgs)
					await MultitaskingTabsHelpers.AddNewTabWithParameterAsync(typeof(PaneHolderPage), paneArgs);
				// The navigation parameter is for the custom page
				else if (parameter is CustomTabViewItemParameter tabArgs)
					await MultitaskingTabsHelpers.AddNewTabWithParameterAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
			}

			if (isThemeInitialized)
			{
				// Load the app theme resources
				_resourcesService.LoadAppResources(_appearanceSettingsService);

				// Load the drives
				await Task.WhenAll(
					_drivesViewModel.UpdateDrivesAsync(),
					_networkDrivesViewModel.UpdateDrivesAsync());
			}
		}

		public void NotifyChanges()
		{
			OnPropertyChanged(nameof(ShouldViewControlBeDisplayed));
			OnPropertyChanged(nameof(ShouldPreviewPaneBeActive));
			OnPropertyChanged(nameof(ShouldPreviewPaneBeDisplayed));
		}

		private void NavigateToNumberedTabKeyboardAccelerator(KeyboardAcceleratorInvokedEventArgs? e)
		{
			int indexToSelect = e!.KeyboardAccelerator.Key switch
			{
				VirtualKey.Number1 => 0,
				VirtualKey.Number2 => 1,
				VirtualKey.Number3 => 2,
				VirtualKey.Number4 => 3,
				VirtualKey.Number5 => 4,
				VirtualKey.Number6 => 5,
				VirtualKey.Number7 => 6,
				VirtualKey.Number8 => 7,
				// Select the last tab
				VirtualKey.Number9 => CurrentInstanceTabBarItems.Count - 1,
				_ => CurrentInstanceTabBarItems.Count - 1,
			};

			// Only select the tab if it is in the list
			if (indexToSelect < CurrentInstanceTabBarItems.Count)
				App.AppModel.TabStripSelectedIndex = indexToSelect;

			e!.Handled = true;
		}

		private async Task OpenNewWindowAcceleratorAsync(KeyboardAcceleratorInvokedEventArgs? e)
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.App.AppLaunchAlias));

			e!.Handled = true;
		}
	}
}
