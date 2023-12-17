// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;

namespace Files.App.Helpers
{
	public static class NavigationHelpers
	{
		private static MainPageViewModel MainPageViewModel { get; } = Ioc.Default.GetRequiredService<MainPageViewModel>();
		private static DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private static NetworkDrivesViewModel NetworkDrivesViewModel { get; } = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();

		public static Task OpenPathInNewTab(string? path)
		{
			return AddNewTabByPathAsync(typeof(PaneHolderPage), path);
		}

		public static Task AddNewTabAsync()
		{
			return AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
		}

		public static async Task AddNewTabByPathAsync(Type type, string? path, int atIndex = -1)
		{
			if (string.IsNullOrEmpty(path))
			{
				path = "Home";
			}
			// Support drives launched through jump list by stripping away the question mark at the end.
			else if (path.EndsWith("\\?"))
			{
				path = path.Remove(path.Length - 1);
			}

			var tabItem = new TabBarItem()
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

			await UpdateTabInfoAsync(tabItem, path);

			var index = atIndex == -1 ? MainPageViewModel.AppInstances.Count : atIndex;

			MainPageViewModel.AppInstances.Insert(index, tabItem);

			App.AppModel.TabStripSelectedIndex = index;
		}

		public static async Task AddNewTabByParamAsync(Type type, object tabViewItemArgs, int atIndex = -1)
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

			tabItem.ContentChanged += Control_ContentChanged;

			await UpdateTabInfoAsync(tabItem, tabViewItemArgs);

			var index = atIndex == -1 ? MainPageViewModel.AppInstances.Count : atIndex;
			MainPageViewModel.AppInstances.Insert(index, tabItem);
			App.AppModel.TabStripSelectedIndex = index;
		}

		private static async Task UpdateTabInfoAsync(TabBarItem tabItem, object navigationArg)
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
			if (result.Item1 is not null)
			{
				var navigationParameter = tabItem.NavigationParameter.NavigationParameter;
				var a1 = navigationParameter is PaneNavigationArguments pna1 ? pna1 : new PaneNavigationArguments() { LeftPaneNavPathParam = navigationParameter as string };
				var a2 = navigationArg is PaneNavigationArguments pna2 ? pna2 : new PaneNavigationArguments() { LeftPaneNavPathParam = navigationArg as string };

				if (a1 == a2)
					(tabItem.Header, tabItem.IconSource, tabItem.ToolTipText) = result;
			}
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
			else if (WSLDistroManager.TryGetDistro(currentPath, out WslDistroItem? wslDistro) && currentPath.Equals(wslDistro.Path))
			{
				tabLocationHeader = wslDistro.Text;
				iconSource.ImageSource = new BitmapImage(wslDistro.Icon);
			}
			else
			{
				var normalizedCurrentPath = PathNormalization.NormalizePath(currentPath);
				var matchingCloudDrive = CloudDrivesManager.Drives.FirstOrDefault(x => normalizedCurrentPath.Equals(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
				if (matchingCloudDrive is not null)
				{
					iconSource.ImageSource = matchingCloudDrive.Icon;
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
				var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(currentPath, 16u, Windows.Storage.FileProperties.ThumbnailMode.ListView, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale, true);
				if (iconData is not null)
					iconSource.ImageSource = await iconData.ToBitmapAsync();
			}

			return (tabLocationHeader, iconSource, toolTipText);
		}

		public static async Task UpdateInstancePropertiesAsync(object? navigationArg)
		{
			await SafetyExtensions.IgnoreExceptions(async () =>
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

				if (MainPageViewModel.AppInstances.Count > 1)
					windowTitle = $"{windowTitle} ({MainPageViewModel.AppInstances.Count})";

				if (navigationArg == MainPageViewModel.SelectedTabItem?.NavigationParameter?.NavigationParameter)
					MainWindow.Instance.AppWindow.Title = $"{windowTitle} - Files";
			});
		}

		public static async void Control_ContentChanged(object? sender, CustomTabViewItemParameter e)
		{
			if (sender is null)
				return;

			var matchingTabItem = MainPageViewModel.AppInstances.SingleOrDefault(x => x == (TabBarItem)sender);
			if (matchingTabItem is null)
				return;

			await UpdateTabInfoAsync(matchingTabItem, e.NavigationParameter);
		}

		public static Task<bool> OpenPathInNewWindowAsync(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return Task.FromResult(false);

			var folderUri = new Uri($"files-uwp:?folder={Uri.EscapeDataString(path)}");

			return Launcher.LaunchUriAsync(folderUri).AsTask();
		}

		public static Task<bool> OpenTabInNewWindowAsync(string tabArgs)
		{
			var folderUri = new Uri($"files-uwp:?tab={Uri.EscapeDataString(tabArgs)}");
			return Launcher.LaunchUriAsync(folderUri).AsTask();
		}

		public static void OpenInSecondaryPane(IShellPage associatedInstance, ListedItem listedItem)
		{
			if (associatedInstance is null || listedItem is null)
				return;

			associatedInstance.PaneHolder?.OpenPathInNewPane((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
		}

		public static Task LaunchNewWindowAsync()
		{
			var filesUWPUri = new Uri("files-uwp:");
			return Launcher.LaunchUriAsync(filesUWPUri).AsTask();
		}

		public static async Task OpenSelectedItemsAsync(IShellPage associatedInstance, bool openViaApplicationPicker = false)
		{
			// Don't open files and folders inside recycle bin
			if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal) ||
				associatedInstance.SlimContentPage?.SelectedItems is null)
			{
				return;
			}

			var forceOpenInNewTab = false;
			var selectedItems = associatedInstance.SlimContentPage.SelectedItems.ToList();
			var opened = false;

			// If multiple files are selected, open them together
			if (!openViaApplicationPicker &&
				selectedItems.Count > 1 &&
				selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && !x.IsExecutable && !x.IsShortcut))
			{
				opened = await Win32Helpers.InvokeWin32ComponentAsync(string.Join('|', selectedItems.Select(x => x.ItemPath)), associatedInstance);
			}

			if (opened)
				return;

			foreach (ListedItem item in selectedItems)
			{
				var type = item.PrimaryItemAttribute == StorageItemTypes.Folder
					? FilesystemItemType.Directory
					: FilesystemItemType.File;

				await OpenPath(item.ItemPath, associatedInstance, type, false, openViaApplicationPicker, forceOpenInNewTab: forceOpenInNewTab);

				if (type == FilesystemItemType.Directory)
					forceOpenInNewTab = true;
			}
		}

		public static async Task OpenItemsWithExecutableAsync(IShellPage associatedInstance, IEnumerable<IStorageItemWithPath> items, string executablePath)
		{
			// Don't open files and folders inside recycle bin
			if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal) ||
				associatedInstance.SlimContentPage is null)
				return;

			var arguments = string.Join(" ", items.Select(item => $"\"{item.Path}\""));
			await Win32Helpers.InvokeWin32ComponentAsync(executablePath, associatedInstance, arguments);
		}

		/// <summary>
		/// Navigates to a directory or opens file
		/// </summary>
		/// <param name="path">The path to navigate to or open</param>
		/// <param name="associatedInstance">The instance associated with view</param>
		/// <param name="itemType"></param>
		/// <param name="openSilent">Determines whether history of opened item is saved (... to Recent Items/Windows Timeline/opening in background)</param>
		/// <param name="openViaApplicationPicker">Determines whether open file using application picker</param>
		/// <param name="selectItems">List of filenames that are selected upon navigation</param>
		/// <param name="forceOpenInNewTab">Open folders in a new tab regardless of the "OpenFoldersInNewTab" option</param>
		public static async Task<bool> OpenPath(string path, IShellPage associatedInstance, FilesystemItemType? itemType = null, bool openSilent = false, bool openViaApplicationPicker = false, IEnumerable<string>? selectItems = null, string? args = default, bool forceOpenInNewTab = false)
		{
			string previousDir = associatedInstance.FilesystemViewModel.WorkingDirectory;
			bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
			bool isDirectory = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory);
			bool isReparsePoint = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.ReparsePoint);
			bool isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(path);
			bool isScreenSaver = FileExtensionHelpers.IsScreenSaverFile(path);
			bool isTag = path.StartsWith("tag:");
			FilesystemResult opened = (FilesystemResult)false;

			if (isTag)
			{
				if (!forceOpenInNewTab)
				{
					associatedInstance.NavigateToPath(path, new NavigationArguments()
					{
						IsSearchResultPage = true,
						SearchPathParam = "Home",
						SearchQuery = path,
						AssociatedTabInstance = associatedInstance,
						NavPathParam = path
					});
				}
				else
				{
					await NavigationHelpers.OpenPathInNewTab(path);
				}

				return true;
			}

			var shortcutInfo = new ShellLinkItem();
			if (itemType is null || isShortcut || isHiddenItem || isReparsePoint)
			{
				if (isShortcut)
				{
					var shInfo = await FileOperationsHelpers.ParseLinkAsync(path);

					if (shInfo is null)
						return false;

					itemType = shInfo.IsFolder ? FilesystemItemType.Directory : FilesystemItemType.File;

					shortcutInfo = shInfo;

					if (shortcutInfo.InvalidTarget)
					{
						if (await DialogDisplayHelper.ShowDialogAsync(DynamicDialogFactory.GetFor_ShortcutNotFound(shortcutInfo.TargetPath)) != DynamicDialogResult.Primary)
							return false;

						// Delete shortcut
						var shortcutItem = StorageHelpers.FromPathAndType(path, FilesystemItemType.File);
						await associatedInstance.FilesystemHelpers.DeleteItemAsync(shortcutItem, DeleteConfirmationPolicies.Never, false, true);
					}
				}
				else if (isReparsePoint)
				{
					if (!isDirectory &&
						NativeFindStorageItemHelper.GetWin32FindDataForPath(path, out var findData) &&
						findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK)
					{
						shortcutInfo.TargetPath = NativeFileOperationsHelper.ParseSymLink(path);
					}
					itemType ??= isDirectory ? FilesystemItemType.Directory : FilesystemItemType.File;
				}
				else if (isHiddenItem)
				{
					itemType = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory) ? FilesystemItemType.Directory : FilesystemItemType.File;
				}
				else
				{
					itemType = await StorageHelpers.GetTypeFromPath(path);
				}
			}

			switch (itemType)
			{
				case FilesystemItemType.Library:
					opened = await OpenLibrary(path, associatedInstance, selectItems, forceOpenInNewTab);
					break;

				case FilesystemItemType.Directory:
					opened = await OpenDirectory(path, associatedInstance, selectItems, shortcutInfo, forceOpenInNewTab);
					break;

				case FilesystemItemType.File:
					// Starts the screensaver in full-screen mode
					if (isScreenSaver)
						args += "/s";

					opened = await OpenFile(path, associatedInstance, shortcutInfo, openViaApplicationPicker, args);
					break;
			};

			if (opened.ErrorCode == FileSystemStatusCode.NotFound && !openSilent)
			{
				await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
				associatedInstance.ToolbarViewModel.CanRefresh = false;
				associatedInstance.FilesystemViewModel?.RefreshItems(previousDir);
			}

			return opened;
		}

		private static async Task<FilesystemResult> OpenLibrary(string path, IShellPage associatedInstance, IEnumerable<string>? selectItems, bool forceOpenInNewTab)
		{
			IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			var opened = (FilesystemResult)false;
			bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
			if (isHiddenItem)
			{
				await OpenPath(forceOpenInNewTab, UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab, path, associatedInstance);
				opened = (FilesystemResult)true;
			}
			else if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
			{
				opened = (FilesystemResult)await library.CheckDefaultSaveFolderAccess();
				if (opened)
					await OpenPathAsync(forceOpenInNewTab, UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab, path, library.Text, associatedInstance, selectItems);
			}
			return opened;
		}

		private static async Task<FilesystemResult> OpenDirectory(string path, IShellPage associatedInstance, IEnumerable<string>? selectItems, ShellLinkItem shortcutInfo, bool forceOpenInNewTab)
		{
			IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			var opened = (FilesystemResult)false;
			bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
			bool isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(path);

			if (isShortcut)
			{
				if (string.IsNullOrEmpty(shortcutInfo.TargetPath))
				{
					await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance);
					opened = (FilesystemResult)true;
				}
				else
				{
					await OpenPath(forceOpenInNewTab, UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab, shortcutInfo.TargetPath, associatedInstance, selectItems);
					opened = (FilesystemResult)true;
				}
			}
			else if (isHiddenItem)
			{
				await OpenPath(forceOpenInNewTab, UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab, path, associatedInstance);
				opened = (FilesystemResult)true;
			}
			else
			{
				opened = await associatedInstance.FilesystemViewModel.GetFolderWithPathFromPathAsync(path)
					.OnSuccess((childFolder) =>
					{
						// Add location to Recent Items List
						if (childFolder.Item is SystemStorageFolder)
							App.RecentItemsManager.AddToRecentItems(childFolder.Path);
					});
				if (!opened)
					opened = (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path);

				if (opened)
					await OpenPath(forceOpenInNewTab, UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab, path, associatedInstance, selectItems);
				else
					await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance);
			}
			return opened;
		}

		private static async Task<FilesystemResult> OpenFile(string path, IShellPage associatedInstance, ShellLinkItem shortcutInfo, bool openViaApplicationPicker = false, string? args = default)
		{
			var opened = (FilesystemResult)false;
			bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
			bool isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(path) || !string.IsNullOrEmpty(shortcutInfo.TargetPath);

			if (isShortcut)
			{
				if (string.IsNullOrEmpty(shortcutInfo.TargetPath))
				{
					await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance, args);
				}
				else
				{
					if (!FileExtensionHelpers.IsWebLinkFile(path))
					{
						StorageFileWithPath childFile = await associatedInstance.FilesystemViewModel.GetFileWithPathFromPathAsync(shortcutInfo.TargetPath);
						// Add location to Recent Items List
						if (childFile?.Item is SystemStorageFile)
							App.RecentItemsManager.AddToRecentItems(childFile.Path);
					}
					await Win32Helpers.InvokeWin32ComponentAsync(shortcutInfo.TargetPath, associatedInstance, $"{args} {shortcutInfo.Arguments}", shortcutInfo.RunAsAdmin, shortcutInfo.WorkingDirectory);
				}
				opened = (FilesystemResult)true;
			}
			else if (isHiddenItem)
			{
				await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance, args);
			}
			else
			{
				opened = await associatedInstance.FilesystemViewModel.GetFileWithPathFromPathAsync(path)
					.OnSuccess(async childFile =>
					{
						// Add location to Recent Items List
						if (childFile.Item is SystemStorageFile)
							App.RecentItemsManager.AddToRecentItems(childFile.Path);

						if (openViaApplicationPicker)
						{
							LauncherOptions options = InitializeWithWindow(new LauncherOptions
							{
								DisplayApplicationPicker = true
							});
							if (!await Launcher.LaunchFileAsync(childFile.Item, options))
								await ContextMenu.InvokeVerb("openas", path);
						}
						else
						{
							//try using launcher first
							bool launchSuccess = false;

							BaseStorageFileQueryResult? fileQueryResult = null;

							//Get folder to create a file query (to pass to apps like Photos, Movies & TV..., needed to scroll through the folder like what Windows Explorer does)
							BaseStorageFolder currentFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(path));

							if (currentFolder is not null)
							{
								QueryOptions queryOptions = new(CommonFileQuery.DefaultQuery, null);

								//We can have many sort entries
								SortEntry sortEntry = new()
								{
									AscendingOrder = associatedInstance.InstanceViewModel.FolderSettings.DirectorySortDirection == SortDirection.Ascending
								};

								//Basically we tell to the launched app to follow how we sorted the files in the directory.
								var sortOption = associatedInstance.InstanceViewModel.FolderSettings.DirectorySortOption;

								switch (sortOption)
								{
									case SortOption.Name:
										sortEntry.PropertyName = "System.ItemNameDisplay";
										queryOptions.SortOrder.Clear();
										queryOptions.SortOrder.Add(sortEntry);
										break;

									case SortOption.DateModified:
										sortEntry.PropertyName = "System.DateModified";
										queryOptions.SortOrder.Clear();
										queryOptions.SortOrder.Add(sortEntry);
										break;

									case SortOption.DateCreated:
										sortEntry.PropertyName = "System.DateCreated";
										queryOptions.SortOrder.Clear();
										queryOptions.SortOrder.Add(sortEntry);
										break;

									//Unfortunately this is unsupported | Remarks: https://learn.microsoft.com/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041
									//case Enums.SortOption.Size:

									//sortEntry.PropertyName = "System.TotalFileSize";
									//queryOptions.SortOrder.Clear();
									//queryOptions.SortOrder.Add(sortEntry);
									//break;

									//Unfortunately this is unsupported | Remarks: https://learn.microsoft.com/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041
									//case Enums.SortOption.FileType:

									//sortEntry.PropertyName = "System.FileExtension";
									//queryOptions.SortOrder.Clear();
									//queryOptions.SortOrder.Add(sortEntry);
									//break;

									//Handle unsupported
									default:
										//keep the default one in SortOrder IList
										break;
								}

								var options = InitializeWithWindow(new LauncherOptions());
								if (currentFolder.AreQueryOptionsSupported(queryOptions))
								{
									fileQueryResult = currentFolder.CreateFileQueryWithOptions(queryOptions);
									options.NeighboringFilesQuery = fileQueryResult.ToStorageFileQueryResult();
								}

								// Now launch file with options.
								var storageItem = (StorageFile)await FilesystemTasks.Wrap(() => childFile.Item.ToStorageFileAsync().AsTask());

								if (storageItem is not null)
									launchSuccess = await Launcher.LaunchFileAsync(storageItem, options);
							}

							if (!launchSuccess)
								await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance, args);
						}
					});
			}
			return opened;
		}

		// WINUI3
		private static LauncherOptions InitializeWithWindow(LauncherOptions obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, MainWindow.Instance.WindowHandle);
			return obj;
		}

		private static Task OpenPath(bool forceOpenInNewTab, bool openFolderInNewTabSetting, string path, IShellPage associatedInstance, IEnumerable<string>? selectItems = null)
			=> OpenPathAsync(forceOpenInNewTab, openFolderInNewTabSetting, path, path, associatedInstance, selectItems);

		private static async Task OpenPathAsync(bool forceOpenInNewTab, bool openFolderInNewTabSetting, string path, string text, IShellPage associatedInstance, IEnumerable<string>? selectItems = null)
		{
			if (forceOpenInNewTab || openFolderInNewTabSetting)
			{
				await OpenPathInNewTab(text);
			}
			else
			{
				associatedInstance.ToolbarViewModel.PathControlDisplayText = text;
				associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(path), new NavigationArguments()
				{
					NavPathParam = path,
					AssociatedTabInstance = associatedInstance,
					SelectItems = selectItems
				});
			}
		}
	}
}
