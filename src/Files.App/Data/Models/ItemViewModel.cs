// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Cloud;
using Files.App.Filesystem.Search;
using Files.App.Filesystem.StorageEnumerators;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers.FileListCache;
using Files.App.Shell;
using Files.App.Storage.FtpStorage;
using Files.App.ViewModels.Previews;
using Files.Backend.Services;
using Files.Backend.Services.SizeProvider;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Cloud;
using FluentFTP;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using static Files.App.Helpers.NativeDirectoryChangesHelper;
using static Files.Backend.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.Data.Models
{
	public sealed class ItemViewModel : ObservableObject
	{
		private readonly ConcurrentQueue<uint> gitChangesQueue;
		private readonly ConcurrentDictionary<string, bool> itemLoadQueue;
		private readonly AsyncManualResetEvent gitChangedEvent;
		private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");
		private readonly IFileListCache fileListCache = FileListCacheController.GetInstance();
		private readonly string folderTypeTextLocalized = "Folder".GetLocalizedResource();
		private Task? gitProcessQueueAction;
		private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();
		private CancellationTokenSource addFilesCTS;
		private CancellationTokenSource semaphoreCTS;
		private CancellationTokenSource loadPropsCTS;
		private CancellationTokenSource searchCTS;

		public event EventHandler GitDirectoryUpdated;
		public string? GitDirectory { get; private set; }

		public ItemViewModel()
		{
			gitChangesQueue = new ConcurrentQueue<uint>();
			itemLoadQueue = new ConcurrentDictionary<string, bool>();
			addFilesCTS = new CancellationTokenSource();
			semaphoreCTS = new CancellationTokenSource();
			loadPropsCTS = new CancellationTokenSource();
			gitChangedEvent = new AsyncManualResetEvent();
			enumFolderSemaphore = new SemaphoreSlim(1, 1);
		}

		private async void FolderSizeProvider_SizeChanged(object? sender, SizeChangedEventArgs e)
		{
			try
			{
				await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				var matchingItem = filesAndFolders.FirstOrDefault(x => x.ItemPath == e.Path);
				if (matchingItem is not null)
				{
					await dispatcherQueue.EnqueueOrInvokeAsync(() =>
					{
						if (e.ValueState is SizeChangedValueState.None)
						{
							matchingItem.FileSizeBytes = 0;
							matchingItem.FileSize = "ItemSizeNotCalculated".GetLocalizedResource();
						}
						else if (e.ValueState is SizeChangedValueState.Final || (long)e.NewSize > matchingItem.FileSizeBytes)
						{
							matchingItem.FileSizeBytes = (long)e.NewSize;
							matchingItem.FileSize = e.NewSize.ToSizeString();
						}

						DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
					},
					Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
				}
			}
			finally
			{
				enumFolderSemaphore.Release();
			}
		}

		public void CancelExtendedPropertiesLoading()
		{
			loadPropsCTS.Cancel();
			loadPropsCTS = new CancellationTokenSource();
		}

		public void CancelExtendedPropertiesLoadingForItem(StandardItemViewModel item)
		{
			itemLoadQueue.TryUpdate(item.ItemPath, true, false);
		}

		public Task ReloadItemGroupHeaderImagesAsync()
		{
			// This is needed to update the group icons for file type groups
			if (folderSettings.DirectoryGroupOption != GroupOption.FileType || FilesAndFolders.GroupedCollection is null)
				return Task.CompletedTask;

			return Task.Run(async () =>
			{
				foreach (var gp in FilesAndFolders.GroupedCollection.ToList())
				{
					var img = await GetItemTypeGroupIcon(gp.FirstOrDefault());
					await dispatcherQueue.EnqueueOrInvokeAsync(() =>
					{
						gp.Model.ImageSource = img;
					}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
				}
			});
		}

		// ThumbnailSize is set to 96 so that unless we override it, mode is in turn set to SingleItem
		private async Task LoadItemThumbnail(StandardItemViewModel item, uint thumbnailSize = 96, IStorageItem? matchingStorageItem = null)
		{
			var wasIconLoaded = false;
			if (item.IsLibrary || item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
			{
				if (UserSettingsService.FoldersSettingsService.ShowThumbnails &&
					!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
				{
					var matchingStorageFile = matchingStorageItem?.AsBaseStorageFile() ?? await GetFileFromPathAsync(item.ItemPath);

					if (matchingStorageFile is not null)
					{
						// SingleItem returns image thumbnails in the correct aspect ratio for the grid layouts
						// ListView is used for the details and columns layout
						var thumbnailMode = thumbnailSize < 96 ? ThumbnailMode.ListView : ThumbnailMode.SingleItem;

						using StorageItemThumbnail Thumbnail = await FilesystemTasks.Wrap(() => matchingStorageFile.GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ResizeThumbnail).AsTask());

						if (!(Thumbnail is null || Thumbnail.Size == 0 || Thumbnail.OriginalHeight == 0 || Thumbnail.OriginalWidth == 0))
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								item.FileImage ??= new BitmapImage();
								item.FileImage.DecodePixelType = DecodePixelType.Logical;
								item.FileImage.DecodePixelWidth = (int)thumbnailSize;
								await item.FileImage.SetSourceAsync(Thumbnail);
								if (!string.IsNullOrEmpty(item.FileExtension) &&
									!item.IsShortcut && !item.IsExecutable &&
									!ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant()))
								{
									DefaultIcons.AddIfNotPresent(item.FileExtension.ToLowerInvariant(), item.FileImage);
								}
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
							wasIconLoaded = true;
						}

						var overlayInfo = await FileThumbnailHelper.LoadOverlayAsync(item.ItemPath, thumbnailSize);
						if (overlayInfo is not null)
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								item.IconOverlay = await overlayInfo.ToBitmapAsync();
								shieldIcon ??= await UIHelpers.GetShieldIconResource();
								item.ShieldIcon = shieldIcon;
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
						}
					}
				}

				if (!wasIconLoaded)
				{
					var iconInfo = await FileThumbnailHelper.LoadIconAndOverlayAsync(item.ItemPath, thumbnailSize, false);
					if (iconInfo.IconData is not null)
					{
						await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							item.FileImage = await iconInfo.IconData.ToBitmapAsync();
							if (!string.IsNullOrEmpty(item.FileExtension) &&
								!item.IsShortcut && !item.IsExecutable &&
								!ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant()))
							{
								DefaultIcons.AddIfNotPresent(item.FileExtension.ToLowerInvariant(), item.FileImage);
							}
						}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
					}

					if (iconInfo.OverlayData is not null)
					{
						await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							item.IconOverlay = await iconInfo.OverlayData.ToBitmapAsync();
							shieldIcon ??= await UIHelpers.GetShieldIconResource();
							item.ShieldIcon = shieldIcon;
						}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
					}
				}
			}
			else
			{
				if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
				{
					var matchingStorageFolder = matchingStorageItem?.AsBaseStorageFolder() ?? await GetFolderFromPathAsync(item.ItemPath);
					if (matchingStorageFolder is not null)
					{
						// SingleItem returns image thumbnails in the correct aspect ratio for the grid layouts
						// ListView is used for the details and columns layout
						var thumbnailMode = thumbnailSize < 96 ? ThumbnailMode.ListView : ThumbnailMode.SingleItem;

						// We use ReturnOnlyIfCached because otherwise folders thumbnails have a black background, this has the downside the folder previews don't work
						using StorageItemThumbnail Thumbnail = await FilesystemTasks.Wrap(() => matchingStorageFolder.GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ReturnOnlyIfCached).AsTask());
						if (!(Thumbnail is null || Thumbnail.Size == 0 || Thumbnail.OriginalHeight == 0 || Thumbnail.OriginalWidth == 0))
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								item.FileImage ??= new BitmapImage();
								item.FileImage.DecodePixelType = DecodePixelType.Logical;
								item.FileImage.DecodePixelWidth = (int)thumbnailSize;
								await item.FileImage.SetSourceAsync(Thumbnail);
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
							wasIconLoaded = true;
						}

						var overlayInfo = await FileThumbnailHelper.LoadOverlayAsync(item.ItemPath, thumbnailSize);
						if (overlayInfo is not null)
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								item.IconOverlay = await overlayInfo.ToBitmapAsync();
								shieldIcon ??= await UIHelpers.GetShieldIconResource();
								item.ShieldIcon = shieldIcon;
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
						}
					}
				}

				if (!wasIconLoaded)
				{
					var iconInfo = await FileThumbnailHelper.LoadIconAndOverlayAsync(item.ItemPath, thumbnailSize, true);
					if (iconInfo.IconData is not null)
					{
						await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							item.FileImage = await iconInfo.IconData.ToBitmapAsync();
						}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
					}

					if (iconInfo.OverlayData is not null)
					{
						await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							item.IconOverlay = await iconInfo.OverlayData.ToBitmapAsync();
							shieldIcon ??= await UIHelpers.GetShieldIconResource();
							item.ShieldIcon = shieldIcon;
						}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
					}
				}
			}
		}

		public async Task LoadExtendedItemProperties(StandardItemViewModel item, uint thumbnailSize = 20)
		{
			if (item is null)
				return;

			itemLoadQueue[item.ItemPath] = false;

			var cts = loadPropsCTS;

			try
			{
				await Task.Run(async () =>
				{
					if (itemLoadQueue.TryGetValue(item.ItemPath, out var canceled) && canceled)
						return;

					var wasSyncStatusLoaded = false;
					var loadGroupHeaderInfo = false;
					ImageSource? groupImage = null;
					GroupedCollection<StandardItemViewModel>? gp = null;
					try
					{
						var isFileTypeGroupMode = folderSettings.DirectoryGroupOption == GroupOption.FileType;
						BaseStorageFile? matchingStorageFile = null;
						if (item.Key is not null && FilesAndFolders.IsGrouped && FilesAndFolders.GetExtendedGroupHeaderInfo is not null)
						{
							gp = FilesAndFolders.GroupedCollection?.Where(x => x.Model.Key == item.Key).FirstOrDefault();
							loadGroupHeaderInfo = gp is not null && !gp.Model.Initialized && gp.GetExtendedGroupHeaderInfo is not null;
						}

						if (item.IsLibrary || item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
						{
							if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
							{
								cts.Token.ThrowIfCancellationRequested();
								matchingStorageFile = await GetFileFromPathAsync(item.ItemPath);
								if (matchingStorageFile is not null)
								{
									cts.Token.ThrowIfCancellationRequested();
									await LoadItemThumbnail(item, thumbnailSize, matchingStorageFile);

									var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageFile);
									var fileFRN = await FileTagsHelper.GetFileFRN(matchingStorageFile);
									var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);
									var itemType = (item.ItemType == "Folder".GetLocalizedResource()) ? item.ItemType : matchingStorageFile.DisplayType;
									cts.Token.ThrowIfCancellationRequested();

									await dispatcherQueue.EnqueueOrInvokeAsync(() =>
									{
										item.FolderRelativeId = matchingStorageFile.FolderRelativeId;
										item.ItemType = itemType;
										item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
										item.FileFRN = fileFRN;
										item.FileTags = fileTag;
									},
									Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

									SetFileTag(item);
									wasSyncStatusLoaded = true;
								}
							}

							if (!wasSyncStatusLoaded)
								await LoadItemThumbnail(item, thumbnailSize, null);
						}
						else
						{
							if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
							{
								cts.Token.ThrowIfCancellationRequested();
								BaseStorageFolder matchingStorageFolder = await GetFolderFromPathAsync(item.ItemPath);
								if (matchingStorageFolder is not null)
								{
									cts.Token.ThrowIfCancellationRequested();
									await LoadItemThumbnail(item, thumbnailSize, matchingStorageFolder);
									if (matchingStorageFolder.DisplayName != item.Name && !matchingStorageFolder.DisplayName.StartsWith("$R", StringComparison.Ordinal))
									{
										cts.Token.ThrowIfCancellationRequested();
										await dispatcherQueue.EnqueueOrInvokeAsync(() =>
										{
											item.ItemNameRaw = matchingStorageFolder.DisplayName;
										});
										await fileListCache.SaveFileDisplayNameToCache(item.ItemPath, matchingStorageFolder.DisplayName);
										if (folderSettings.DirectorySortOption == SortOption.Name && !isLoadingItems)
										{
											await OrderFilesAndFoldersAsync();
											await ApplySingleFileChangeAsync(item);
										}
									}

									cts.Token.ThrowIfCancellationRequested();
									var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageFolder);
									var fileFRN = await FileTagsHelper.GetFileFRN(matchingStorageFolder);
									var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);
									var itemType = (item.ItemType == "Folder".GetLocalizedResource()) ? item.ItemType : matchingStorageFolder.DisplayType;
									cts.Token.ThrowIfCancellationRequested();

									await dispatcherQueue.EnqueueOrInvokeAsync(() =>
									{
										item.FolderRelativeId = matchingStorageFolder.FolderRelativeId;
										item.ItemType = itemType;
										item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
										item.FileFRN = fileFRN;
										item.FileTags = fileTag;
									},
									Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

									SetFileTag(item);
									wasSyncStatusLoaded = true;
								}
							}
							if (!wasSyncStatusLoaded)
							{
								cts.Token.ThrowIfCancellationRequested();
								await LoadItemThumbnail(item, thumbnailSize, null);
							}
						}

						if (loadGroupHeaderInfo && isFileTypeGroupMode)
						{
							cts.Token.ThrowIfCancellationRequested();
							groupImage = await GetItemTypeGroupIcon(item, matchingStorageFile);
						}
					}
					catch (Exception)
					{
					}
					finally
					{
						if (!wasSyncStatusLoaded)
						{
							cts.Token.ThrowIfCancellationRequested();
							await FilesystemTasks.Wrap(async () =>
							{
								var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);

								await dispatcherQueue.EnqueueOrInvokeAsync(() =>
								{
									// Reset cloud sync status icon
									item.SyncStatusUI = new CloudDriveSyncStatusUI();

									item.FileTags = fileTag;
								},
								Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

								SetFileTag(item);
							});
						}

						if (loadGroupHeaderInfo)
						{
							cts.Token.ThrowIfCancellationRequested();
							await SafetyExtensions.IgnoreExceptions(() =>
								dispatcherQueue.EnqueueOrInvokeAsync(() =>
								{
									gp.Model.ImageSource = groupImage;
									gp.InitializeExtendedGroupHeaderInfoAsync();
								}));
						}
					}
				}, cts.Token);
			}
			catch (OperationCanceledException)
			{
				// Ignored
			}
			finally
			{
				itemLoadQueue.TryRemove(item.ItemPath, out _);
			}
		}


		private async Task RapidAddItemsToCollectionAsync(string path, string? previousDir, Action postLoadCallback)
		{
			CancelLoadAndClearFiles();

			if (string.IsNullOrEmpty(path))
				return;

			try
			{
				// Only one instance at a time should access this function
				// Wait here until the previous one has ended
				// If we're waiting and a new update request comes through simply drop this instance
				await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				// Drop all the other waiting instances
				semaphoreCTS.Cancel();
				semaphoreCTS = new CancellationTokenSource();

				IsLoadingItems = true;

				filesAndFolders.Clear();
				FilesAndFolders.Clear();

				if (path.ToLowerInvariant().EndsWith(ShellLibraryItem.EXTENSION, StringComparison.Ordinal))
				{
					if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library) && !library.IsEmpty)
					{
						var libItem = new LibraryItem(library);
						foreach (var folder in library.Folders)
							await RapidAddItemsToCollection(folder, libItem);
					}
				}
				else
				{
					await RapidAddItemsToCollection(path);
				}

				IsLoadingItems = false;

				AdaptiveLayoutHelpers.ApplyAdaptativeLayout(folderSettings, WorkingDirectory, filesAndFolders);

				if (Ioc.Default.GetRequiredService<PreviewPaneViewModel>().IsEnabled)
				{
					// Find and select README file
					foreach (var item in filesAndFolders)
					{
						if (item.PrimaryItemAttribute == StorageItemTypes.File && item.Name.Contains("readme", StringComparison.OrdinalIgnoreCase))
						{
							OnSelectionRequestedEvent?.Invoke(this, new List<StandardItemViewModel>() { item });
							break;
						}
					}
				}
			}
			finally
			{
				// Make sure item count is updated
				enumFolderSemaphore.Release();
			}

			postLoadCallback?.Invoke();
		}

		public async Task EnumerateItemsFromSpecialFolderAsync(string path)
		{
			var isFtp = FtpHelpers.IsFtpPath(path);

			
		}

		public async Task<int> EnumerateItemsFromStandardFolderAsync(string path, CancellationToken cancellationToken, LibraryItem? library = null)
		{
			// Flag to use FindFirstFileExFromApp or StorageFolder enumeration - Use storage folder for Box Drive (#4629)
			var isBoxFolder = App.CloudDrivesManager.Drives.FirstOrDefault(x => x.Text == "Box")?.Path?.TrimEnd('\\') is string boxFolder && path.StartsWith(boxFolder);
			bool isWslDistro = App.WSLDistroManager.TryGetDistro(path, out _);
			bool isNetwork = path.StartsWith(@"\\", StringComparison.Ordinal) &&
				!path.StartsWith(@"\\?\", StringComparison.Ordinal) &&
				!path.StartsWith(@"\\SHELL\", StringComparison.Ordinal) &&
				!isWslDistro;
			bool enumFromStorageFolder = isBoxFolder;

			BaseStorageFolder? rootFolder = null;

			if (isNetwork)
			{
				var auth = await NetworkDrivesAPI.AuthenticateNetworkShare(path);
				if (!auth)
					return -1;
			}

			if (!enumFromStorageFolder && FolderHelpers.CheckFolderAccessWithWin32(path))
			{
				// Will enumerate with FindFirstFileExFromApp, rootFolder only used for Bitlocker
				currentStorageFolder = null;
			}
			else if (workingRoot is not null)
			{
				var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path, workingRoot, currentStorageFolder));
				if (!res)
					return -1;

				currentStorageFolder = res.Result;
				rootFolder = currentStorageFolder.Item;
				enumFromStorageFolder = true;
			}
			else
			{
				var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
				if (res)
				{
					currentStorageFolder = res.Result;
					rootFolder = currentStorageFolder.Item;
				}
				else if (res == FileSystemStatusCode.Unauthorized)
				{
					await DialogDisplayHelper.ShowDialogAsync(
						"AccessDenied".GetLocalizedResource(),
						"AccessDeniedToFolder".GetLocalizedResource());

					return -1;
				}
				else if (res == FileSystemStatusCode.NotFound)
				{
					await DialogDisplayHelper.ShowDialogAsync(
						"FolderNotFoundDialog/Title".GetLocalizedResource(),
						"FolderNotFoundDialog/Text".GetLocalizedResource());

					return -1;
				}
				else
				{
					await DialogDisplayHelper.ShowDialogAsync(
						"DriveUnpluggedDialog/Title".GetLocalizedResource(),
						res.ErrorCode.ToString());

					return -1;
				}
			}

			var pathRoot = Path.GetPathRoot(path);
			if (Path.IsPathRooted(path) && pathRoot == path)
			{
				rootFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
				if (await FolderHelpers.CheckBitlockerStatusAsync(rootFolder, WorkingDirectory))
					await ContextMenu.InvokeVerb("unlock-bde", pathRoot);
			}

			if (enumFromStorageFolder)
			{
				var basicProps = await rootFolder?.GetBasicPropertiesAsync();
				var currentFolder = library ?? new StandardItemViewModel(rootFolder?.FolderRelativeId ?? string.Empty)
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemPropertiesInitialized = true,
					ItemNameRaw = rootFolder?.DisplayName ?? string.Empty,
					ItemDateModifiedReal = basicProps.DateModified,
					ItemType = rootFolder?.DisplayType ?? string.Empty,
					FileImage = null,
					LoadFileIcon = false,
					ItemPath = string.IsNullOrEmpty(rootFolder?.Path) ? currentStorageFolder?.Path ?? string.Empty : rootFolder.Path,
					FileSize = null,
					FileSizeBytes = 0,
				};

				if (library is null)
					currentFolder.ItemDateCreatedReal = rootFolder?.DateCreated ?? DateTimeOffset.Now;

				CurrentFolder = currentFolder;
				await EnumFromStorageFolderAsync(path, rootFolder, currentStorageFolder, cancellationToken);

				// Workaround for #7428
				return isBoxFolder ? 2 : 1;
			}
			else
			{
				(IntPtr hFile, WIN32_FIND_DATA findData, int errorCode) = await Task.Run(() =>
				{
					var findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
					var additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

					IntPtr hFileTsk = FindFirstFileExFromApp(
						path + "\\*.*",
						findInfoLevel,
						out WIN32_FIND_DATA findDataTsk,
						FINDEX_SEARCH_OPS.FindExSearchNameMatch,
						IntPtr.Zero,
						additionalFlags);

					return (hFileTsk, findDataTsk, hFileTsk.ToInt64() == -1 ? Marshal.GetLastWin32Error() : 0);
				})
				.WithTimeoutAsync(TimeSpan.FromSeconds(5));

				var itemModifiedDate = DateTime.Now;
				var itemCreatedDate = DateTime.Now;

				try
				{
					FileTimeToSystemTime(ref findData.ftLastWriteTime, out var systemModifiedTimeOutput);
					itemModifiedDate = systemModifiedTimeOutput.ToDateTime();

					FileTimeToSystemTime(ref findData.ftCreationTime, out SYSTEMTIME systemCreatedTimeOutput);
					itemCreatedDate = systemCreatedTimeOutput.ToDateTime();
				}
				catch (ArgumentException)
				{
				}

				var isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
				var opacity = isHidden ? Constants.UI.DimItemOpacity : 1d;

				var currentFolder = library ?? new StandardItemViewModel(null)
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemPropertiesInitialized = true,
					ItemNameRaw = Path.GetFileName(path.TrimEnd('\\')),
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
					ItemType = folderTypeTextLocalized,
					FileImage = null,
					IsHiddenItem = isHidden,
					Opacity = opacity,
					LoadFileIcon = false,
					ItemPath = path,
					FileSize = null,
					FileSizeBytes = 0,
				};

				CurrentFolder = currentFolder;

				if (hFile == IntPtr.Zero)
				{
					await DialogDisplayHelper.ShowDialogAsync("DriveUnpluggedDialog/Title".GetLocalizedResource(), "");

					return -1;
				}
				else if (hFile.ToInt64() == -1)
				{
					await EnumFromStorageFolderAsync(path, rootFolder, currentStorageFolder, cancellationToken);

					// errorCode == ERROR_ACCESS_DENIED
					if (!filesAndFolders.Any() && errorCode == 0x5)
					{
						await DialogDisplayHelper.ShowDialogAsync(
							"AccessDenied".GetLocalizedResource(),
							"AccessDeniedToFolder".GetLocalizedResource());

						return -1;
					}

					return 1;
				}
				else
				{
					await Task.Run(async () =>
					{
						List<StandardItemViewModel> fileList = await Win32StorageEnumerator.ListEntries(path, hFile, findData, cancellationToken, -1, intermediateAction: async (intermediateList) =>
						{
							filesAndFolders.AddRange(intermediateList);
							await OrderFilesAndFoldersAsync();
							await ApplyFilesAndFoldersChangesAsync();
						}, defaultIconPairs: DefaultIcons);

						filesAndFolders.AddRange(fileList);
						await OrderFilesAndFoldersAsync();
						await ApplyFilesAndFoldersChangesAsync();
					});

					return 0;
				}
			}
		}

		private async Task<CloudDriveSyncStatus> CheckCloudDriveSyncStatusAsync(IStorageItem item)
		{
			int? syncStatus = null;
			if (item is BaseStorageFile file && file.Properties is not null)
			{
				var extraProperties = await FilesystemTasks.Wrap(() => file.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus" }).AsTask());
				if (extraProperties)
					syncStatus = (int?)(uint?)extraProperties.Result["System.FilePlaceholderStatus"];
			}
			else if (item is BaseStorageFolder folder && folder.Properties is not null)
			{
				var extraProperties = await FilesystemTasks.Wrap(() => folder.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus", "System.FileOfflineAvailabilityStatus" }).AsTask());
				if (extraProperties)
				{
					syncStatus = (int?)(uint?)extraProperties.Result["System.FileOfflineAvailabilityStatus"];

					// If no FileOfflineAvailabilityStatus, check FilePlaceholderStatus
					syncStatus ??= (int?)(uint?)extraProperties.Result["System.FilePlaceholderStatus"];
				}
			}

			if (syncStatus is null || !Enum.IsDefined(typeof(CloudDriveSyncStatus), syncStatus))
				return CloudDriveSyncStatus.Unknown;

			return (CloudDriveSyncStatus)syncStatus;
		}

		

		private void WatchForGitChanges(bool hasSyncStatus)
		{
			var hWatchDir = NativeFileOperationsHelper.CreateFileFromApp(
				GitDirectory!,
				1,
				1 | 2 | 4,
				IntPtr.Zero,
				3,
				(uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics | (uint)NativeFileOperationsHelper.File_Attributes.Overlapped,
				IntPtr.Zero);

			if (hWatchDir.ToInt64() == -1)
				return;

			gitProcessQueueAction ??= Task.Factory.StartNew(() => ProcessGitChangesQueue(watcherCTS.Token), default,
				TaskCreationOptions.LongRunning, TaskScheduler.Default);

			var gitWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
			{
				var buff = new byte[4096];
				var rand = Guid.NewGuid();
				var notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_SIZE | FILE_NOTIFY_CHANGE_CREATION;

				if (hasSyncStatus)
					notifyFilters |= FILE_NOTIFY_CHANGE_ATTRIBUTES;

				var overlapped = new OVERLAPPED();
				overlapped.hEvent = CreateEvent(IntPtr.Zero, false, false, null);
				const uint INFINITE = 0xFFFFFFFF;

				while (x.Status != AsyncStatus.Canceled)
				{
					unsafe
					{
						fixed (byte* pBuff = buff)
						{
							ref var notifyInformation = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[0]);
							if (x.Status == AsyncStatus.Canceled)
								break;

							ReadDirectoryChangesW(hWatchDir, pBuff,
								4096, true,
								notifyFilters, null,
								ref overlapped, null);

							if (x.Status == AsyncStatus.Canceled)
								break;

							var rc = WaitForSingleObjectEx(overlapped.hEvent, INFINITE, true);

							uint offset = 0;
							ref var notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
							if (x.Status == AsyncStatus.Canceled)
								break;

							do
							{
								notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);

								uint action = notifyInfo.Action;

								gitChangesQueue.Enqueue(action);

								offset += notifyInfo.NextEntryOffset;
							}
							while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

							gitChangedEvent.Set();
						}
					}
				}

				CloseHandle(overlapped.hEvent);
				gitChangesQueue.Clear();
			});

			watcherCTS.Token.Register(() =>
			{
				if (gitWatcherAction is not null)
				{
					gitWatcherAction?.Cancel();

					// Prevent duplicate execution of this block
					gitWatcherAction = null;
				}

				CancelIoEx(hWatchDir, IntPtr.Zero);
				CloseHandle(hWatchDir);
			});
		}

		private async Task ProcessGitChangesQueue(CancellationToken cancellationToken)
		{
			const int DELAY = 200;
			var sampler = new IntervalSampler(100);
			int changes = 0;

			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					if (await gitChangedEvent.WaitAsync(DELAY, cancellationToken))
					{
						gitChangedEvent.Reset();
						while (gitChangesQueue.TryDequeue(out var _))
							++changes;

						if (changes != 0 && sampler.CheckNow())
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(() => GitDirectoryUpdated?.Invoke(null, null!));
							changes = 0;
						}
					}
				}
			}
			catch { }
		}
		
		public async Task AddSearchResultsToCollection(ObservableCollection<StandardItemViewModel> searchItems, string currentSearchPath)
		{
			filesAndFolders.Clear();
			filesAndFolders.AddRange(searchItems);

			await OrderFilesAndFoldersAsync();
			await ApplyFilesAndFoldersChangesAsync();
		}

		public async Task SearchAsync(FolderSearch search)
		{
			ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting });

			CancelSearch();
			searchCTS = new CancellationTokenSource();
			filesAndFolders.Clear();
			IsLoadingItems = true;
			IsSearchResults = true;
			await ApplyFilesAndFoldersChangesAsync();
			EmptyTextType = EmptyTextType.None;

			ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress });

			var results = new List<StandardItemViewModel>();
			search.SearchTick += async (s, e) =>
			{
				filesAndFolders = new List<StandardItemViewModel>(results);
				await OrderFilesAndFoldersAsync();
				await ApplyFilesAndFoldersChangesAsync();
			};

			await search.SearchAsync(results, searchCTS.Token);

			filesAndFolders = new List<StandardItemViewModel>(results);

			await OrderFilesAndFoldersAsync();
			await ApplyFilesAndFoldersChangesAsync();

			ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete });

		}
	}
}
