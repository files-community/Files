// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
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
		private readonly IUserSettingsService _userSettingsService;
		private readonly IAppearanceSettingsService _appearanceSettingsService;
		private readonly DrivesViewModel _drivesViewModel;
		private readonly NetworkDrivesViewModel _networkDrivesViewModel;
		private readonly IResourcesService _resourcesService;

		public static ObservableCollection<TabBarItem> CurrentInstanceTabBarItems { get; } = new();

		public ITabBar? CurrentInstanceTabBar { get; set; }

		// NOTE: This is not used for now because multi windowing is not supported
		public List<ITabBar> AllInstanceTabBars { get; } = new();

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
			// Dependency injections
			_userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			_appearanceSettingsService = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
			_drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
			_networkDrivesViewModel = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();
			_resourcesService = Ioc.Default.GetRequiredService<IResourcesService>();

			// Create commands
			NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(NavigateToNumberedTabKeyboardAccelerator);
			OpenNewWindowAcceleratorCommand = new AsyncRelayCommand<KeyboardAcceleratorInvokedEventArgs>(OpenNewWindowAcceleratorAsync);
		}

		public async Task OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back)
				return;

			//Initialize the static theme helper to capture a reference to this window
			//to handle theme changes without restarting the app
			var isInitialized = ThemeHelper.Initialize();

			var parameter = e.Parameter;
			var ignoreStartupSettings = false;
			if (parameter is MainPageNavigationArguments mainPageNavigationArguments)
			{
				parameter = mainPageNavigationArguments.Parameter;
				ignoreStartupSettings = mainPageNavigationArguments.IgnoreStartupSettings;
			}

			if (parameter is null || (parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
			{
				try
				{
					// add last session tabs to closed tabs stack if those tabs are not about to be opened
					if (!_userSettingsService.AppSettingsService.RestoreTabsOnStartup && !_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp && _userSettingsService.GeneralSettingsService.LastSessionTabList != null)
					{
						var items = new CustomTabViewItemParameter[_userSettingsService.GeneralSettingsService.LastSessionTabList.Count];

						for (int i = 0; i < items.Length; i++)
							items[i] = CustomTabViewItemParameter.Deserialize(_userSettingsService.GeneralSettingsService.LastSessionTabList[i]);

						BaseTabBar.PushRecentTab(items);
					}

					if (_userSettingsService.AppSettingsService.RestoreTabsOnStartup)
					{
						_userSettingsService.AppSettingsService.RestoreTabsOnStartup = false;
						if (_userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in _userSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
								await MultitaskingTabsHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}

							if (!_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
								_userSettingsService.GeneralSettingsService.LastSessionTabList = null;
						}
					}
					else if (_userSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
						_userSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
					{
						foreach (string path in _userSettingsService.GeneralSettingsService.TabsOnStartupList)
							await MultitaskingTabsHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), path);
					}
					else if (_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						_userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						foreach (string tabArgsString in _userSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
							await MultitaskingTabsHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
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
						await MultitaskingTabsHelpers.AddNewTabAsync();
					}
				}
				catch
				{
					await MultitaskingTabsHelpers.AddNewTabAsync();
				}
			}
			else
			{
				if (!ignoreStartupSettings)
				{
					try
					{
						if (_userSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
								_userSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
						{
							foreach (string path in _userSettingsService.GeneralSettingsService.TabsOnStartupList)
								await MultitaskingTabsHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), path);
						}
						else if (_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
							_userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in _userSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
								await MultitaskingTabsHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}

							var defaultArg = new CustomTabViewItemParameter() { InitialPageType = typeof(PaneHolderPage), NavigationParameter = "Home" };

							_userSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
						}
					}
					catch
					{
					}
				}

				if (parameter is string navArgs)
					await MultitaskingTabsHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
				else if (parameter is PaneNavigationArguments paneArgs)
					await MultitaskingTabsHelpers.AddNewTabByParamAsync(typeof(PaneHolderPage), paneArgs);
				else if (parameter is CustomTabViewItemParameter tabArgs)
					await MultitaskingTabsHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
			}

			if (isInitialized)
			{
				// Load the app theme resources
				_resourcesService.LoadAppResources(_appearanceSettingsService);

				await Task.WhenAll(
					_drivesViewModel.UpdateDrivesAsync(),
					_networkDrivesViewModel.UpdateDrivesAsync());
			}
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

			e.Handled = true;
		}

		private async Task OpenNewWindowAcceleratorAsync(KeyboardAcceleratorInvokedEventArgs? e)
		{
			var filesUWPUri = new Uri("files-uwp:");
			await Launcher.LaunchUriAsync(filesUWPUri);

			e!.Handled = true;
		}
	}
}
