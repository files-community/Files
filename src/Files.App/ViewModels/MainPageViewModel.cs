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
			OpenNewWindowAcceleratorCommand = new AsyncRelayCommand<KeyboardAcceleratorInvokedEventArgs>(OpenNewWindowAccelerator);
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

		private async Task OpenNewWindowAccelerator(KeyboardAcceleratorInvokedEventArgs? e)
		{
			var filesUWPUri = new Uri("files-uwp:");
			await Launcher.LaunchUriAsync(filesUWPUri);

			e!.Handled = true;
		}

		public async Task AddNewTabByPathAsync(Type type, string? path, int atIndex = -1)
		{
			if (string.IsNullOrEmpty(path))
				path = "Home";
			// Support drives launched through jump list by stripping away the question mark at the end.
			else if (path.EndsWith("\\?"))
				path = path.Remove(path.Length - 1);

			var tabItem = new TabBarItem
			{
				Header = null,
				IconSource = null,
				Description = null,
				ToolTipText = null,
				NavigationParameter = new CustomTabViewItemParameter()
				{
					InitialPageType = type,
					NavigationParameter = path
				}
			};

			tabItem.ContentChanged += Control_ContentChanged;

			await UpdateTabInfo(tabItem, path);

			var index = atIndex == -1 ? CurrentInstanceTabBarItems.Count : atIndex;
			CurrentInstanceTabBarItems.Insert(index, tabItem);
			App.AppModel.TabStripSelectedIndex = index;
		}

		public async Task UpdateInstanceProperties(object navigationArg)
		{
			string windowTitle = string.Empty;

			if (navigationArg is PaneNavigationArguments paneArgs)
			{
				if (!string.IsNullOrEmpty(paneArgs.LeftPaneNavPathParam) && !string.IsNullOrEmpty(paneArgs.RightPaneNavPathParam))
				{
					var leftTabInfo = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
					var rightTabInfo = await GetSelectedTabInfoAsync(paneArgs.RightPaneNavPathParam);

					windowTitle = $"{leftTabInfo.tabLocationHeader} | {rightTabInfo.tabLocationHeader}";
				}
				else
				{
					(windowTitle, _, _) = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
				}
			}
			else if (navigationArg is string pathArgs)
			{
				(windowTitle, _, _) = await GetSelectedTabInfoAsync(pathArgs);
			}

			if (CurrentInstanceTabBarItems.Count > 1)
				windowTitle = $"{windowTitle} ({CurrentInstanceTabBarItems.Count})";

			if (navigationArg == SelectedTabBarItem?.NavigationParameter?.NavigationParameter)
				MainWindow.Instance.AppWindow.Title = $"{windowTitle} - Files";
		}

		public async Task UpdateTabInfo(TabBarItem tabItem, object navigationArg)
		{
			tabItem.AllowStorageItemDrop = true;

			(string, IconSource, string) result = (null, null, null);

			if (navigationArg is PaneNavigationArguments paneArgs)
			{
				if (!string.IsNullOrEmpty(paneArgs.LeftPaneNavPathParam) && !string.IsNullOrEmpty(paneArgs.RightPaneNavPathParam))
				{
					var leftTabInfo = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
					var rightTabInfo = await GetSelectedTabInfoAsync(paneArgs.RightPaneNavPathParam);

					result = ($"{leftTabInfo.tabLocationHeader} | {rightTabInfo.tabLocationHeader}",
						leftTabInfo.tabIcon,
						$"{leftTabInfo.toolTipText} | {rightTabInfo.toolTipText}");
				}
				else
				{
					result = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
				}
			}
			else if (navigationArg is string pathArgs)
			{
				result = await GetSelectedTabInfoAsync(pathArgs);
			}

			// Don't update tabItem if the contents of the tab have already changed
			if (result.Item1 is not null && navigationArg == tabItem.NavigationParameter.NavigationParameter)
				(tabItem.Header, tabItem.IconSource, tabItem.ToolTipText) = result;
		}

		public async Task<(string tabLocationHeader, IconSource tabIcon, string toolTipText)> GetSelectedTabInfoAsync(string currentPath)
		{
			string? tabLocationHeader;
			var iconSource = new ImageIconSource();
			string toolTipText = currentPath;

			if (string.IsNullOrEmpty(currentPath) || currentPath == "Home")
			{
				tabLocationHeader = "Home".GetLocalizedResource();

				iconSource.ImageSource = new BitmapImage(new Uri(Constants.FluentIconsPaths.HomeIcon));
			}
			else if (currentPath.Equals(Constants.UserEnvironmentPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "Desktop".GetLocalizedResource();
			}
			else if (currentPath.Equals(Constants.UserEnvironmentPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "Downloads".GetLocalizedResource();
			}
			else if (currentPath.Equals(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "RecycleBin".GetLocalizedResource();

				// Use 48 for higher resolution, the other items look fine with 16.
				var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(currentPath, 48u, Windows.Storage.FileProperties.ThumbnailMode.ListView, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale, true);
				if (iconData is not null)
					iconSource.ImageSource = await iconData.ToBitmapAsync();
			}
			else if (currentPath.Equals(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "ThisPC".GetLocalizedResource();
			}
			else if (currentPath.Equals(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "SidebarNetworkDrives".GetLocalizedResource();
			}
			else if (App.LibraryManager.TryGetLibrary(currentPath, out LibraryLocationItem library))
			{
				var libName = System.IO.Path.GetFileNameWithoutExtension(library.Path).GetLocalizedResource();
				// If localized string is empty use the library name.
				tabLocationHeader = string.IsNullOrEmpty(libName) ? library.Text : libName;
			}
			else if (App.WSLDistroManager.TryGetDistro(currentPath, out WslDistroItem? wslDistro) && currentPath.Equals(wslDistro.Path))
			{
				tabLocationHeader = wslDistro.Text;
				iconSource.ImageSource = new BitmapImage(wslDistro.Icon);
			}
			else
			{
				var normalizedCurrentPath = PathNormalization.NormalizePath(currentPath);

				var matchingCloudDrive = App.CloudDrivesManager.Drives.FirstOrDefault(x => normalizedCurrentPath.Equals(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
				if (matchingCloudDrive is not null)
				{
					iconSource.ImageSource = matchingCloudDrive.Icon;
					tabLocationHeader = matchingCloudDrive.Text;
				}
				else if (PathNormalization.NormalizePath(PathNormalization.GetPathRoot(currentPath)) == normalizedCurrentPath) // If path is a drive's root
				{
					var matchingDrive = _networkDrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(netDrive => normalizedCurrentPath.Contains(PathNormalization.NormalizePath(netDrive.Path), StringComparison.OrdinalIgnoreCase));
					matchingDrive ??= _drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(drive => normalizedCurrentPath.Contains(PathNormalization.NormalizePath(drive.Path), StringComparison.OrdinalIgnoreCase));
					tabLocationHeader = matchingDrive is not null ? matchingDrive.Text : normalizedCurrentPath;
				}
				else
				{
					tabLocationHeader = currentPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();

					FilesystemResult<StorageFolderWithPath> rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(currentPath));

					if (rootItem)
					{
						BaseStorageFolder currentFolder = await FilesystemTasks.Wrap(
							() => StorageFileExtensions.DangerousGetFolderFromPathAsync(currentPath, rootItem));

						if (currentFolder is not null && !string.IsNullOrEmpty(currentFolder.DisplayName))
							tabLocationHeader = currentFolder.DisplayName;
					}
				}
			}

			if (iconSource.ImageSource is null)
			{
				var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(currentPath, 16u, Windows.Storage.FileProperties.ThumbnailMode.ListView, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale, true);
				if (iconData is not null)
					iconSource.ImageSource = await iconData.ToBitmapAsync();
			}

			return (tabLocationHeader, iconSource, toolTipText);
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
								await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}

							if (!_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
								_userSettingsService.GeneralSettingsService.LastSessionTabList = null;
						}
					}
					else if (_userSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
						_userSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
					{
						foreach (string path in _userSettingsService.GeneralSettingsService.TabsOnStartupList)
							await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
					}
					else if (_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						_userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						foreach (string tabArgsString in _userSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
							await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationParameter);
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
						await AddNewTabAsync();
					}
				}
				catch
				{
					await AddNewTabAsync();
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
								await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
						}
						else if (_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
							_userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in _userSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
								await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationParameter);
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
					await AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
				else if (parameter is PaneNavigationArguments paneArgs)
					await AddNewTabByParam(typeof(PaneHolderPage), paneArgs);
				else if (parameter is CustomTabViewItemParameter tabArgs)
					await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationParameter);
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

		public Task AddNewTabAsync()
		{
			return AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
		}

		public async Task AddNewTabByParam(Type type, object tabViewItemArgs, int atIndex = -1)
		{
			var tabItem = new TabBarItem
			{
				Header = null,
				IconSource = null,
				Description = null,
				ToolTipText = null,
				NavigationParameter = new CustomTabViewItemParameter()
				{
					InitialPageType = type,
					NavigationParameter = tabViewItemArgs
				}
			};

			tabItem.ContentChanged += Control_ContentChanged;

			await UpdateTabInfo(tabItem, tabViewItemArgs);

			var index = atIndex == -1 ? CurrentInstanceTabBarItems.Count : atIndex;
			CurrentInstanceTabBarItems.Insert(index, tabItem);
			App.AppModel.TabStripSelectedIndex = index;
		}

		public async void Control_ContentChanged(object? sender, CustomTabViewItemParameter e)
		{
			if (sender is null)
				return;

			var matchingTabItem = CurrentInstanceTabBarItems.SingleOrDefault(x => x == (TabBarItem)sender);
			if (matchingTabItem is null)
				return;

			await UpdateTabInfo(matchingTabItem, e.NavigationParameter);
		}
	}
}
