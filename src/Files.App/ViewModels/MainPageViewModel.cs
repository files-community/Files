// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using Files.App.UserControls.MultitaskingControl;
using Files.App.Views;
using Files.Backend.Services;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Windows.Input;
using Windows.System;

namespace Files.App.ViewModels
{
	public class MainPageViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; }

		private IAppearanceSettingsService AppearanceSettingsService { get; }

		private DrivesViewModel DrivesViewModel { get; }

		private NetworkDrivesViewModel NetworkDrivesViewModel { get; }

		private IResourcesService ResourcesService { get; }

		public IMultitaskingControl? MultitaskingControl { get; set; }

		public List<IMultitaskingControl> MultitaskingControls { get; } = new();

		public static ObservableCollection<TabItem> AppInstances { get; private set; } = new();

		private TabItem? selectedTabItem;
		public TabItem? SelectedTabItem
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
			// Service & ViewModel Initializations
			UserSettingsService = userSettings;
			AppearanceSettingsService = appearanceSettings;
			DrivesViewModel = drivesViewModel;
			NetworkDrivesViewModel = networkDrivesViewModel;
			ResourcesService = resources;

			// Command Initializations
			NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(NavigateToNumberedTabKeyboardAccelerator);
			OpenNewWindowAcceleratorCommand = new AsyncRelayCommand<KeyboardAcceleratorInvokedEventArgs>(OpenNewWindowAccelerator);
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
			else if (path.EndsWith("\\?")) // Support drives launched through jump list by stripping away the question mark at the end.
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
			else if (currentPath.Equals(CommonPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "Desktop".GetLocalizedResource();
			}
			else if (currentPath.Equals(CommonPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "Downloads".GetLocalizedResource();
			}
			else if (currentPath.Equals(CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "RecycleBin".GetLocalizedResource();
			}
			else if (currentPath.Equals(CommonPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "ThisPC".GetLocalizedResource();
			}
			else if (currentPath.Equals(CommonPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
			{
				tabLocationHeader = "SidebarNetworkDrives".GetLocalizedResource();
			}
			else if (App.LibraryManager.TryGetLibrary(currentPath, out LibraryLocationItem library))
			{
				var libName = System.IO.Path.GetFileNameWithoutExtension(library.Path).GetLocalizedResource();
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
					var matchingDrive = NetworkDrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(netDrive => normalizedCurrentPath.Contains(PathNormalization.NormalizePath(netDrive.Path), StringComparison.OrdinalIgnoreCase));
					matchingDrive ??= DrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(drive => normalizedCurrentPath.Contains(PathNormalization.NormalizePath(drive.Path), StringComparison.OrdinalIgnoreCase));
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

			if (DrivesViewModel.Drives.Count == 0)
				await DrivesViewModel.UpdateDrivesAsync();

			//Initialize the static theme helper to capture a reference to this window
			//to handle theme changes without restarting the app
			ThemeHelper.Initialize();

			if (e.Parameter is null || (e.Parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
			{
				try
				{
					// add last session tabs to closed tabs stack if those tabs are not about to be opened
					if (!UserSettingsService.AppSettingsService.RestoreTabsOnStartup && !UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp && UserSettingsService.GeneralSettingsService.LastSessionTabList != null)
					{
						var items = new TabItemArguments[UserSettingsService.GeneralSettingsService.LastSessionTabList.Count];
						for (int i = 0; i < items.Length; i++)
							items[i] = TabItemArguments.Deserialize(UserSettingsService.GeneralSettingsService.LastSessionTabList[i]);

						BaseMultitaskingControl.PushRecentTab(items);
					}

					if (UserSettingsService.AppSettingsService.RestoreTabsOnStartup)
					{
						UserSettingsService.AppSettingsService.RestoreTabsOnStartup = false;
						if (UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = TabItemArguments.Deserialize(tabArgsString);
								await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
							}

							if (!UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
								UserSettingsService.GeneralSettingsService.LastSessionTabList = null;
						}
					}
					else if (UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
						UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
					{
						foreach (string path in UserSettingsService.GeneralSettingsService.TabsOnStartupList)
							await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
					}
					else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						foreach (string tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = TabItemArguments.Deserialize(tabArgsString);
							await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
						}

						var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "Home" };

						UserSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
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
					if (UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
							UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
					{
						foreach (string path in UserSettingsService.GeneralSettingsService.TabsOnStartupList)
							await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
					}
					else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						foreach (string tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = TabItemArguments.Deserialize(tabArgsString);
							await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
						}

						var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "Home" };

						UserSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
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
			ResourcesService.LoadAppResources(AppearanceSettingsService);
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
