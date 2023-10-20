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
	public class MainPageViewModel : ObservableObject
	{
		private IUserSettingsService userSettingsService;
		private IAppearanceSettingsService appearanceSettingsService;
		private readonly DrivesViewModel drivesViewModel;
		private readonly NetworkDrivesViewModel networkDrivesViewModel;
		private IResourcesService resourcesService;

		public ITabBar? MultitaskingControl { get; set; }

		public List<ITabBar> MultitaskingControls { get; } = new List<ITabBar>();

		public static ObservableCollection<Files.App.UserControls.TabBar.TabBarItem> AppInstances { get; private set; } = new ObservableCollection<Files.App.UserControls.TabBar.TabBarItem>();

		private Files.App.UserControls.TabBar.TabBarItem? selectedTabItem;
		public Files.App.UserControls.TabBar.TabBarItem? SelectedTabItem
		{
			get => selectedTabItem;
			set => SetProperty(ref selectedTabItem, value);
		}

		public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand { get; private set; }
		public IAsyncRelayCommand OpenNewWindowAcceleratorCommand { get; private set; }

		public MainPageViewModel(
			IUserSettingsService userSettings,
			IAppearanceSettingsService appearanceSettings,
			IResourcesService resources,
			DrivesViewModel drivesViewModel,
			NetworkDrivesViewModel networkDrivesViewModel)
		{
			userSettingsService = userSettings;
			appearanceSettingsService = appearanceSettings;
			this.drivesViewModel = drivesViewModel;
			this.networkDrivesViewModel = networkDrivesViewModel;
			resourcesService = resources;
			// Create commands
			NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(NavigateToNumberedTabKeyboardAccelerator);
			OpenNewWindowAcceleratorCommand = new AsyncRelayCommand<KeyboardAcceleratorInvokedEventArgs>(OpenNewWindowAcceleratorAsync);
		}

		private void NavigateToNumberedTabKeyboardAccelerator(KeyboardAcceleratorInvokedEventArgs? e)
		{
			int indexToSelect = 0;
			switch (e!.KeyboardAccelerator.Key)
			{
				case VirtualKey.Number1:
					indexToSelect = 0;
					break;

				case VirtualKey.Number2:
					indexToSelect = 1;
					break;

				case VirtualKey.Number3:
					indexToSelect = 2;
					break;

				case VirtualKey.Number4:
					indexToSelect = 3;
					break;

				case VirtualKey.Number5:
					indexToSelect = 4;
					break;

				case VirtualKey.Number6:
					indexToSelect = 5;
					break;

				case VirtualKey.Number7:
					indexToSelect = 6;
					break;

				case VirtualKey.Number8:
					indexToSelect = 7;
					break;

				case VirtualKey.Number9:
					// Select the last tab
					indexToSelect = AppInstances.Count - 1;
					break;
			}

			// Only select the tab if it is in the list
			if (indexToSelect < AppInstances.Count)
				App.AppModel.TabStripSelectedIndex = indexToSelect;
			e.Handled = true;
		}

		private async Task OpenNewWindowAcceleratorAsync(KeyboardAcceleratorInvokedEventArgs? e)
		{
			var filesUWPUri = new Uri("files-uwp:");
			await Launcher.LaunchUriAsync(filesUWPUri);
			e!.Handled = true;
		}

		public async Task AddNewTabByPathAsync(Type type, string? path, int atIndex = -1)
		{
			if (string.IsNullOrEmpty(path))
				path = "Home";
			else if (path.EndsWith("\\?")) // Support drives launched through jump list by stripping away the question mark at the end.
				path = path.Remove(path.Length - 1);

			var tabItem = new Files.App.UserControls.TabBar.TabBarItem()
			{
				Header = null,
				IconSource = null,
				Description = null,
				ToolTipText = null
			};
			tabItem.NavigationParameter = new CustomTabViewItemParameter()
			{
				InitialPageType = type,
				NavigationParameter = path
			};
			tabItem.ContentChanged += Control_ContentChangedAsync;
			await UpdateTabInfoAsync(tabItem, path);
			var index = atIndex == -1 ? AppInstances.Count : atIndex;
			AppInstances.Insert(index, tabItem);
			App.AppModel.TabStripSelectedIndex = index;
		}

		public async Task UpdateInstancePropertiesAsync(object navigationArg)
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

			if (AppInstances.Count > 1)
				windowTitle = $"{windowTitle} ({AppInstances.Count})";

			if (navigationArg == SelectedTabItem?.NavigationParameter?.NavigationParameter)
				MainWindow.Instance.AppWindow.Title = $"{windowTitle} - Files";
		}

		public async Task UpdateTabInfoAsync(Files.App.UserControls.TabBar.TabBarItem tabItem, object navigationArg)
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
					var matchingDrive = networkDrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(netDrive => normalizedCurrentPath.Contains(PathNormalization.NormalizePath(netDrive.Path), StringComparison.OrdinalIgnoreCase));
					matchingDrive ??= drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(drive => normalizedCurrentPath.Contains(PathNormalization.NormalizePath(drive.Path), StringComparison.OrdinalIgnoreCase));
					tabLocationHeader = matchingDrive is not null ? matchingDrive.Text : normalizedCurrentPath;
				}
				else
				{
					tabLocationHeader = currentPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();

					FilesystemResult<StorageFolderWithPath> rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(currentPath));
					if (rootItem)
					{
						BaseStorageFolder currentFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(currentPath, rootItem));
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

		public async Task OnNavigatedToAsync(NavigationEventArgs e)
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
					if (!userSettingsService.AppSettingsService.RestoreTabsOnStartup && !userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp && userSettingsService.GeneralSettingsService.LastSessionTabList != null)
					{
						var items = new CustomTabViewItemParameter[userSettingsService.GeneralSettingsService.LastSessionTabList.Count];
						for (int i = 0; i < items.Length; i++)
							items[i] = CustomTabViewItemParameter.Deserialize(userSettingsService.GeneralSettingsService.LastSessionTabList[i]);

						BaseTabBar.PushRecentTab(items);
					}

					if (userSettingsService.AppSettingsService.RestoreTabsOnStartup)
					{
						userSettingsService.AppSettingsService.RestoreTabsOnStartup = false;
						if (userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in userSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
								await AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}

							if (!userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
								userSettingsService.GeneralSettingsService.LastSessionTabList = null;
						}
					}
					else if (userSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
						userSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
					{
						foreach (string path in userSettingsService.GeneralSettingsService.TabsOnStartupList)
							await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
					}
					else if (userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						foreach (string tabArgsString in userSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
							await AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
						}

						var defaultArg = new CustomTabViewItemParameter() { InitialPageType = typeof(PaneHolderPage), NavigationParameter = "Home" };

						userSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
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
						if (userSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
								userSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
						{
							foreach (string path in userSettingsService.GeneralSettingsService.TabsOnStartupList)
								await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
						}
						else if (userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
							userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in userSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
								await AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}

							var defaultArg = new CustomTabViewItemParameter() { InitialPageType = typeof(PaneHolderPage), NavigationParameter = "Home" };

							userSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
						}
					}
					catch { }
				}

				if (parameter is string navArgs)
					await AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
				else if (parameter is PaneNavigationArguments paneArgs)
					await AddNewTabByParamAsync(typeof(PaneHolderPage), paneArgs);
				else if (parameter is CustomTabViewItemParameter tabArgs)
					await AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
			}

			if (isInitialized)
			{
				// Load the app theme resources
				resourcesService.LoadAppResources(appearanceSettingsService);

				await Task.WhenAll(
					drivesViewModel.UpdateDrivesAsync(),
					networkDrivesViewModel.UpdateDrivesAsync());
			}
		}

		public Task AddNewTabAsync()
		{
			return AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
		}

		public async Task AddNewTabByParamAsync(Type type, object tabViewItemArgs, int atIndex = -1)
		{
			var tabItem = new Files.App.UserControls.TabBar.TabBarItem()
			{
				Header = null,
				IconSource = null,
				Description = null,
				ToolTipText = null
			};

			tabItem.NavigationParameter = new CustomTabViewItemParameter()
			{
				InitialPageType = type,
				NavigationParameter = tabViewItemArgs
			};

			tabItem.ContentChanged += Control_ContentChangedAsync;

			await UpdateTabInfoAsync(tabItem, tabViewItemArgs);

			var index = atIndex == -1 ? AppInstances.Count : atIndex;
			AppInstances.Insert(index, tabItem);
			App.AppModel.TabStripSelectedIndex = index;
		}

		public async void Control_ContentChangedAsync(object? sender, CustomTabViewItemParameter e)
		{
			if (sender is null)
				return;

			var matchingTabItem = AppInstances.SingleOrDefault(x => x == (Files.App.UserControls.TabBar.TabBarItem)sender);
			if (matchingTabItem is null)
				return;

			await UpdateTabInfoAsync(matchingTabItem, e.NavigationParameter);
		}
	}
}
