// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using Files.App.UserControls.MultitaskingControl;
using Files.Backend.Services;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Windows.Input;
using Windows.System;

namespace Files.App.ViewModels
{
	public class MainPageViewModel : ObservableObject
	{
		private readonly IUserSettingsService _userSettingsService;

		private readonly IAppearanceSettingsService _appearanceSettingsService;

		private readonly DrivesViewModel _drivesViewModel;

		private readonly NetworkDrivesModel _networkDrivesViewModel;

		private readonly IResourcesService _resourcesService;

		public IMultitaskingControl? MultitaskingControl { get; set; }

		public List<IMultitaskingControl> MultitaskingControls { get; }

		public static ObservableCollection<TabItem> AppInstances { get; } = new();

		private TabItem? _SelectedTabItem;
		public TabItem? SelectedTabItem
		{
			get => _SelectedTabItem;
			set => SetProperty(ref _SelectedTabItem, value);
		}

		public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand { get; private set; }
		public IAsyncRelayCommand OpenNewWindowAcceleratorCommand { get; private set; }

		public MainPageViewModel(IUserSettingsService userSettings, IAppearanceSettingsService appearanceSettings, IResourcesService resources, DrivesViewModel drivesViewModel, NetworkDrivesModel networkDrivesViewModel)
		{
			_userSettingsService = userSettings;
			_appearanceSettingsService = appearanceSettings;
			_drivesViewModel = drivesViewModel;
			_networkDrivesViewModel = networkDrivesViewModel;
			_resourcesService = resources;

			// Initialize
			MultitaskingControls = new();

			// Initialize commands
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
				VirtualKey.Number9 => AppInstances.Count - 1, // Select the last tab
			};

			// Only select the tab if it is in the list
			if (indexToSelect < AppInstances.Count)
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
			// Default page is HomePage
			if (string.IsNullOrEmpty(path))
				path = "Home";
			// Support drives launched through jump list by stripping away the question mark at the end.
			else if (path.EndsWith("\\?"))
				path = path.Remove(path.Length - 1);

			var tabItem = new TabItem()
			{
				Header = null,
				IconSource = null,
				Description = null,
				ToolTipText = null
			};

			tabItem.Control.NavigationArguments = new TabItemArguments()
			{
				InitialPageType = type,
				NavigationArg = path
			};

			tabItem.Control.ContentChanged += Control_ContentChanged;

			await UpdateTabInfo(tabItem, path);

			var index = atIndex == -1 ? AppInstances.Count : atIndex;

			AppInstances.Insert(index, tabItem);
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

			if (AppInstances.Count > 1)
				windowTitle = $"{windowTitle} ({AppInstances.Count})";

			if (navigationArg == SelectedTabItem?.TabItemArguments?.NavigationArg)
				App.GetAppWindow(App.Window).Title = $"{windowTitle} - Files";
		}

		public async Task UpdateTabInfo(TabItem tabItem, object navigationArg)
		{
			tabItem.AllowStorageItemDrop = true;

			if (navigationArg is PaneNavigationArguments paneArgs)
			{
				if (!string.IsNullOrEmpty(paneArgs.LeftPaneNavPathParam) && !string.IsNullOrEmpty(paneArgs.RightPaneNavPathParam))
				{
					var leftTabInfo = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
					var rightTabInfo = await GetSelectedTabInfoAsync(paneArgs.RightPaneNavPathParam);

					tabItem.Header = $"{leftTabInfo.tabLocationHeader} | {rightTabInfo.tabLocationHeader}";
					tabItem.IconSource = leftTabInfo.tabIcon;
				}
				else
				{
					(tabItem.Header, tabItem.IconSource, tabItem.ToolTipText) = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
				}
			}
			else if (navigationArg is string pathArgs)
			{
				(tabItem.Header, tabItem.IconSource, tabItem.ToolTipText) = await GetSelectedTabInfoAsync(pathArgs);
			}
		}

		public async Task<(string tabLocationHeader, Microsoft.UI.Xaml.Controls.IconSource tabIcon, string toolTipText)> GetSelectedTabInfoAsync(string currentPath)
		{
			string? tabLocationHeader;
			var iconSource = new Microsoft.UI.Xaml.Controls.ImageIconSource();
			string toolTipText = currentPath;

			if (string.IsNullOrEmpty(currentPath) || currentPath == "Home")
			{
				tabLocationHeader = "Home".GetLocalizedResource();
				iconSource.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(Constants.FluentIconsPaths.HomeIcon));
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
				var libName = SystemIO.Path.GetFileNameWithoutExtension(library.Path).GetLocalizedResource();
				// If localized string is empty use the library name.
				tabLocationHeader = string.IsNullOrEmpty(libName) ? library.Text : libName;
			}
			else
			{
				var normalizedCurrentPath = PathNormalization.NormalizePath(currentPath);
				var matchingCloudDrive = App.CloudDrivesManager.Drives.FirstOrDefault(x => normalizedCurrentPath.Equals(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
				if (matchingCloudDrive is not null)
				{
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
						BaseStorageFolder currentFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(currentPath, rootItem));
						if (currentFolder is not null && !string.IsNullOrEmpty(currentFolder.DisplayName))
							tabLocationHeader = currentFolder.DisplayName;
					}
				}
			}

			if (iconSource.ImageSource is null)
			{
				var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(currentPath, 24u, Windows.Storage.FileProperties.ThumbnailMode.ListView, true);
				if (iconData is not null)
					iconSource.ImageSource = await iconData.ToBitmapAsync();
			}

			return (tabLocationHeader, iconSource, toolTipText);
		}

		public async Task OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back)
				return;

			if (_drivesViewModel.Drives.Count == 0)
				await _drivesViewModel.UpdateDrivesAsync();

			// Initialize the static theme helper to capture a reference to this window to handle theme changes without restarting the app
			ThemeHelper.Initialize();

			if (e.Parameter is null || (e.Parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
			{
				try
				{
					// Add last session tabs to closed tabs stack if those tabs are not about to be opened
					if (!_userSettingsService.AppSettingsService.RestoreTabsOnStartup && !_userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp && _userSettingsService.GeneralSettingsService.LastSessionTabList != null)
					{
						var items = new TabItemArguments[_userSettingsService.GeneralSettingsService.LastSessionTabList.Count];

						for (int i = 0; i < items.Length; i++)
							items[i] = TabItemArguments.Deserialize(_userSettingsService.GeneralSettingsService.LastSessionTabList[i]);

						BaseMultitaskingControl.PushRecentTab(items);
					}

					if (_userSettingsService.AppSettingsService.RestoreTabsOnStartup)
					{
						_userSettingsService.AppSettingsService.RestoreTabsOnStartup = false;

						if (_userSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in _userSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = TabItemArguments.Deserialize(tabArgsString);

								await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
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
							var tabArgs = TabItemArguments.Deserialize(tabArgsString);

							await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
						}

						var defaultArg = new TabItemArguments()
						{
							InitialPageType = typeof(PaneHolderPage),
							NavigationArg = "Home"
						};

						_userSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
					}
					else
					{
						await AddNewTabAsync();
					}
				}
				catch (Exception)
				{
					await AddNewTabAsync();
				}
			}
			else
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
							var tabArgs = TabItemArguments.Deserialize(tabArgsString);
							await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
						}

						var defaultArg = new TabItemArguments()
						{
							InitialPageType = typeof(PaneHolderPage),
							NavigationArg = "Home"
						};

						_userSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
					}
				}
				catch (Exception) { }

				if (e.Parameter is string navArgs)
					await AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
				else if (e.Parameter is PaneNavigationArguments paneArgs)
					await AddNewTabByParam(typeof(PaneHolderPage), paneArgs);
				else if (e.Parameter is TabItemArguments tabArgs)
					await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
			}

			// Load the app theme resources
			_resourcesService.LoadAppResources(_appearanceSettingsService);
		}

		public Task AddNewTabAsync()
		{
			return AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
		}

		public async Task AddNewTabByParam(Type type, object tabViewItemArgs, int atIndex = -1)
		{
			var tabItem = new TabItem()
			{
				Header = null,
				IconSource = null,
				Description = null,
				ToolTipText = null
			};

			tabItem.Control.NavigationArguments = new TabItemArguments()
			{
				InitialPageType = type,
				NavigationArg = tabViewItemArgs
			};

			tabItem.Control.ContentChanged += Control_ContentChanged;

			await UpdateTabInfo(tabItem, tabViewItemArgs);

			var index = atIndex == -1 ? AppInstances.Count : atIndex;
			AppInstances.Insert(index, tabItem);
			App.AppModel.TabStripSelectedIndex = index;
		}

		public async void Control_ContentChanged(object? sender, TabItemArguments e)
		{
			if (sender is null)
				return;

			var matchingTabItem = AppInstances.SingleOrDefault(x => x.Control == (TabItemControl)sender);
			if (matchingTabItem is null)
				return;

			await UpdateTabInfo(matchingTabItem, e.NavigationArg);
		}
	}
}
