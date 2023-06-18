// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Cloud;
using Files.App.Filesystem.Search;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers.FileListCache;
using Files.App.Storage.FtpStorage;
using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Data.Models
{
	public sealed class ItemViewModel : ObservableObject
	{
		private readonly IFileListCache fileListCache = FileListCacheController.GetInstance();
		private CancellationTokenSource searchCTS;
		
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
	}
}
