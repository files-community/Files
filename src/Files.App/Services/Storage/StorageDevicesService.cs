// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using OwlCore.Storage.System.IO;
using System.IO;
using Windows.Storage;

namespace Files.App.Services
{
	public sealed class RemovableDrivesService : IRemovableDrivesService
	{
		public IStorageDeviceWatcher CreateWatcher()
		{
			return new ServerDriveWatcher();
		}

		public async IAsyncEnumerable<IFolder> GetDrivesAsync()
		{
			// Start the per-drive RPCs off the UI thread, including when the ID list is cached.
			var ids = await Server.DriveService.GetDriveIdsAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
			var pending = ids.Select(GetDriveItemAsync).ToArray();
			await foreach (var completed in Task.WhenEach(pending))
			{
				var item = await SafetyExtensions.IgnoreExceptions(() => completed, App.Logger);
				var pCloudDrivePath = App.AppModel.PCloudDrivePath;
				if (item is null ||
					(!string.IsNullOrEmpty(pCloudDrivePath) && string.Equals(item.Path, pCloudDrivePath, StringComparison.OrdinalIgnoreCase)))
					continue;
				yield return item;
			}
		}

		private static async Task<DriveItem?> GetDriveItemAsync(string id)
		{
			// Read the remote DTO into an unpublished local item before returning to the UI.
			var info = await Server.DriveService.GetDriveAsync(id).AsTask().ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
			return info is null || info.Label == "Google Drive" ? null : DriveItem.FromServer(info);
		}

		public Task<IFolder?> GetPrimaryDriveAsync()
		{
			var cDrivePath = $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\";
			if (!Directory.Exists(cDrivePath))
			{
				App.Logger.LogWarning($"Primary system drive '{cDrivePath}' could not be found.");
				return Task.FromResult<IFolder?>(null);
			}

			return Task.FromResult<IFolder?>(new SystemFolder(cDrivePath));
		}

		public async Task UpdateDrivePropertiesAsync(IFolder drive)
		{
			var rootModified = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Id).AsTask());
			if (rootModified && drive is DriveItem matchingDriveEjected)
			{
				var root = rootModified.Result!;
				_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					matchingDriveEjected.Root = root;
					matchingDriveEjected.Text = root.DisplayName;
					return matchingDriveEjected.UpdatePropertiesAsync();
				});
			}
		}
	}
}
