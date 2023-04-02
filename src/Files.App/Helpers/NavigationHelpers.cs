using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Shell;
using Files.App.ViewModels;
using Files.App.Views;
using Files.Backend.Helpers;
using Files.Backend.Services.Settings;
using Files.Shared;
using Files.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;

namespace Files.App.Helpers
{
	public static class NavigationHelpers
	{
		private static readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public static Task OpenPathInNewTab(string? path)
			=> MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), path);

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

		public static async Task OpenSelectedItems(IShellPage associatedInstance, bool openViaApplicationPicker = false)
		{
			// Don't open files and folders inside recycle bin
			if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal) ||
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

		public static async void OpenItemsWithExecutable(IShellPage associatedInstance, IEnumerable<IStorageItemWithPath> items, string executable)
		{
			// Don't open files and folders inside  recycle bin
			if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal) ||
				associatedInstance.SlimContentPage is null)
			{
				return;
			}

			foreach (var item in items)
			{
				try
				{
					await OpenPath(executable, associatedInstance, FilesystemItemType.File, false, false, args: $"\"{item.Path}\"");
				}
				catch (Exception e)
				{
					// This is to try and figure out the root cause of AppCenter error #985932119u
					App.Logger.LogWarning(e, e.Message);
				}
			}
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
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			var opened = (FilesystemResult)false;
			bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
			if (isHiddenItem)
			{
				await OpenPath(forceOpenInNewTab, userSettingsService.FoldersSettingsService.OpenFoldersInNewTab, path, associatedInstance);
				opened = (FilesystemResult)true;
			}
			else if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
			{
				opened = (FilesystemResult)await library.CheckDefaultSaveFolderAccess();
				if (opened)
					await OpenPath(forceOpenInNewTab, userSettingsService.FoldersSettingsService.OpenFoldersInNewTab, path, library.Text, associatedInstance, selectItems);
			}
			return opened;
		}

		private static async Task<FilesystemResult> OpenDirectory(string path, IShellPage associatedInstance, IEnumerable<string>? selectItems, ShellLinkItem shortcutInfo, bool forceOpenInNewTab)
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

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
					await OpenPath(forceOpenInNewTab, userSettingsService.FoldersSettingsService.OpenFoldersInNewTab, shortcutInfo.TargetPath, associatedInstance, selectItems);
					opened = (FilesystemResult)true;
				}
			}
			else if (isHiddenItem)
			{
				await OpenPath(forceOpenInNewTab, userSettingsService.FoldersSettingsService.OpenFoldersInNewTab, path, associatedInstance);
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
					await OpenPath(forceOpenInNewTab, userSettingsService.FoldersSettingsService.OpenFoldersInNewTab, path, associatedInstance, selectItems);
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

									//Unfortunately this is unsupported | Remarks: https://docs.microsoft.com/en-us/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041
									//case Enums.SortOption.Size:

									//sortEntry.PropertyName = "System.TotalFileSize";
									//queryOptions.SortOrder.Clear();
									//queryOptions.SortOrder.Add(sortEntry);
									//break;

									//Unfortunately this is unsupported | Remarks: https://docs.microsoft.com/en-us/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041
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
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		private static Task OpenPath(bool forceOpenInNewTab, bool openFolderInNewTabSetting, string path, IShellPage associatedInstance, IEnumerable<string>? selectItems = null)
			=> OpenPath(forceOpenInNewTab, openFolderInNewTabSetting, path, path, associatedInstance, selectItems);

		private static async Task OpenPath(bool forceOpenInNewTab, bool openFolderInNewTabSetting, string path, string text, IShellPage associatedInstance, IEnumerable<string>? selectItems = null)
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