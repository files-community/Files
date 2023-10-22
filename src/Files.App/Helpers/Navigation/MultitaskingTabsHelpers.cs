// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for handling <see cref="TabBar"/>.
	/// </summary>
	public static class MultitaskingTabsHelpers
	{
		private static readonly DrivesViewModel _drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private static readonly NetworkDrivesViewModel _networkDrivesViewModel = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();
		private static readonly MainPageViewModel _mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		public static async Task AddNewTabAsync()
		{
			await AddNewTabWithPathAsync(typeof(PaneHolderPage), "Home");
		}

		public static async Task AddNewTabWithPathAsync(Type type, string? path, int atIndex = -1)
		{
			if (string.IsNullOrEmpty(path))
				path = "Home";

			// Support drives launched through jump list by stripping away the question mark at the end.
			if (path.EndsWith("\\?"))
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

			tabItem.ContentChanged += TabViewItemContentFrame_ContentChanged;

			await UpdateTabInfoAsync(tabItem, path);

			var index = atIndex == -1 ? MainPageViewModel.CurrentInstanceTabBarItems.Count : atIndex;

			MainPageViewModel.CurrentInstanceTabBarItems.Insert(index, tabItem);

			App.AppModel.TabStripSelectedIndex = index;
		}

		public static async Task AddNewTabWithParameterAsync(Type type, object tabViewItemArgs, int atIndex = -1)
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

			tabItem.ContentChanged += TabViewItemContentFrame_ContentChanged;

			await UpdateTabInfoAsync(tabItem, tabViewItemArgs);

			var index = atIndex == -1 ? MainPageViewModel.CurrentInstanceTabBarItems.Count : atIndex;
			MainPageViewModel.CurrentInstanceTabBarItems.Insert(index, tabItem);
			App.AppModel.TabStripSelectedIndex = index;
		}

		public static void CloseTabsToTheLeft(TabBarItem clickedTab, ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.CurrentInstanceTabBarItems;
				var currentIndex = tabs.IndexOf(clickedTab);

				tabs.Take(currentIndex).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseTabsToTheRight(TabBarItem clickedTab, ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.CurrentInstanceTabBarItems;
				var currentIndex = tabs.IndexOf(clickedTab);

				tabs.Skip(currentIndex + 1).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseOtherTabs(TabBarItem clickedTab, ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.CurrentInstanceTabBarItems;
				tabs.Where((t) => t != clickedTab).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static Task MoveTabToNewWindow(TabBarItem tab, ITabBar multitaskingControl)
		{
			int index = MainPageViewModel.CurrentInstanceTabBarItems.IndexOf(tab);
			CustomTabViewItemParameter tabItemArguments = MainPageViewModel.CurrentInstanceTabBarItems[index].NavigationParameter;

			multitaskingControl?.CloseTab(MainPageViewModel.CurrentInstanceTabBarItems[index]);

			return tabItemArguments is not null
				? NavigationHelpers.OpenTabInNewWindowAsync(tabItemArguments.Serialize())
				: NavigationHelpers.OpenPathInNewWindowAsync("Home");
		}

		public static async Task UpdateInstancePropertiesAsync(object navigationArg)
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

			if (MainPageViewModel.CurrentInstanceTabBarItems.Count > 1)
				windowTitle = $"{windowTitle} ({MainPageViewModel.CurrentInstanceTabBarItems.Count})";

			if (navigationArg == _mainPageViewModel.SelectedTabBarItem?.NavigationParameter?.NavigationParameter)
				MainWindow.Instance.AppWindow.Title = $"{windowTitle} - Files";
		}

		public static async Task UpdateTabInfoAsync(TabBarItem tabItem, object navigationArg)
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

		public static async Task<(string tabLocationHeader, IconSource tabIcon, string toolTipText)> GetSelectedTabInfoAsync(string currentPath)
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

		public static async void TabViewItemContentFrame_ContentChanged(object? sender, CustomTabViewItemParameter e)
		{
			if (sender is null)
				return;

			var matchingTabItem = MainPageViewModel.CurrentInstanceTabBarItems.SingleOrDefault(x => x == (TabBarItem)sender);
			if (matchingTabItem is null)
				return;

			await UpdateTabInfoAsync(matchingTabItem, e.NavigationParameter);
		}
	}
}
