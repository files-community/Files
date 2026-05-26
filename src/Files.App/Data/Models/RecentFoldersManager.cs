// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using System.Collections.Specialized;
using System.IO;

namespace Files.App.Data.Models
{
	public sealed class RecentFoldersManager
	{
		private const int MaxRecentFolders = 5;

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private readonly SemaphoreSlim updateSyncSemaphore = new(1, 1);

		public List<string> RecentFolders { get; private set; } = [];

		private readonly List<INavigationControlItem> _RecentFolderItems = [];

		[JsonIgnore]
		public IReadOnlyList<INavigationControlItem> RecentFolderItems
		{
			get
			{
				lock (_RecentFolderItems)
					return _RecentFolderItems.ToList().AsReadOnly();
			}
		}

		public async Task LoadAsync()
		{
			await updateSyncSemaphore.WaitAsync();

			try
			{
				var storedFolders = UserSettingsService.GeneralSettingsService.RecentFoldersList;
				var recentFolders = RecentListHelpers.CollapseAndTrim(
					storedFolders?.Where(ShouldTrackPath),
					NormalizeComparisonPath,
					MaxRecentFolders,
					StringComparer.OrdinalIgnoreCase);

				RecentFolders = recentFolders;

				if (storedFolders is null || !storedFolders.SequenceEqual(recentFolders, StringComparer.OrdinalIgnoreCase))
					UserSettingsService.GeneralSettingsService.RecentFoldersList = recentFolders;

				await RebuildRecentFolderItemsAsync();
			}
			finally
			{
				updateSyncSemaphore.Release();
			}
		}

		public async Task RecordPathAsync(string? path)
		{
			if (!ShouldTrackPath(path))
				return;

			await updateSyncSemaphore.WaitAsync();

			try
			{
				var recentFolders = RecentListHelpers.AddOrMoveToFront(
					RecentFolders,
					path!,
					NormalizeComparisonPath,
					MaxRecentFolders,
					StringComparer.OrdinalIgnoreCase);

				if (RecentFolders.SequenceEqual(recentFolders, StringComparer.OrdinalIgnoreCase))
					return;

				RecentFolders = recentFolders;
				UserSettingsService.GeneralSettingsService.RecentFoldersList = recentFolders;

				await RebuildRecentFolderItemsAsync();
			}
			finally
			{
				updateSyncSemaphore.Release();
			}
		}

		private async Task RebuildRecentFolderItemsAsync()
		{
			List<INavigationControlItem> recentFolderItems = [];

			foreach (var path in RecentFolders)
				recentFolderItems.Add(await CreateLocationItemFromPathAsync(path));

			lock (_RecentFolderItems)
			{
				_RecentFolderItems.Clear();
				_RecentFolderItems.AddRange(recentFolderItems);
			}

			DataChanged?.Invoke(SectionType.RecentFolders, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		private async Task<LocationItem> CreateLocationItemFromPathAsync(string path)
		{
			var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(path));
			var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));

			var locationItem = LocationItem.Create<LocationItem>();
			locationItem.Path = path;
			locationItem.Section = SectionType.RecentFolders;
			locationItem.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowProperties = true,
				ShowShellItems = true,
			};
			locationItem.IsDefaultLocation = false;
			locationItem.Text = res?.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\', '/'));

			if (string.IsNullOrWhiteSpace(locationItem.Text))
				locationItem.Text = path;

			if (res is not null && res)
			{
				locationItem.IsInvalid = false;
				if (res.Result is not null)
					await LoadIconForLocationItemAsync(locationItem, res.Result.Path);
			}
			else
			{
				locationItem.IsInvalid = true;
				Debug.WriteLine($"Recent folder item was invalid {res?.ErrorCode}, item: {path}");
				await LoadDefaultIconForLocationItemAsync(locationItem);
			}

			return locationItem;
		}

		private static bool ShouldTrackPath(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return false;

			if (path is "Home" or "ReleaseNotes" or "Settings")
				return false;

			if (path.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				return false;

			if (LibraryManager.IsLibraryPath(path) || ZipStorageFolder.IsZipPath(path))
				return false;

			return Path.IsPathRooted(path) || FtpHelpers.IsFtpPath(path);
		}

		private static string NormalizeComparisonPath(string path)
		{
			return PathNormalization.NormalizePath(path);
		}

		private static async Task LoadIconForLocationItemAsync(LocationItem locationItem, string path)
		{
			try
			{
				var result = await FileThumbnailHelper.GetIconAsync(
					path,
					Constants.ShellIconSizes.Small,
					true,
					IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

				if (result is null)
					return;

				locationItem.IconData = result;

				var bitmapImage = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => locationItem.IconData.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
				if (bitmapImage is not null)
					locationItem.Icon = bitmapImage;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading icon for {path}: {ex.Message}");
			}
		}

		private static async Task LoadDefaultIconForLocationItemAsync(LocationItem locationItem)
		{
			try
			{
				var defaultIcon = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => UIHelpers.GetSidebarIconResource(Constants.ImageRes.Folder));
				if (defaultIcon is not null)
					locationItem.Icon = defaultIcon;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading default icon: {ex.Message}");
			}
		}
	}
}
