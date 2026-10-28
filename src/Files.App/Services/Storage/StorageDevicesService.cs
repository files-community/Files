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
			var ids = await Server.DriveService.GetDriveIdsAsync();
			var pending = ids.Select(id => Server.DriveService.GetDriveAsync(id).AsTask()).ToArray();
			await foreach (var completed in Task.WhenEach(pending))
			{
				var info = await SafetyExtensions.IgnoreExceptions(() => completed, App.Logger);
				var pCloudDrivePath = App.AppModel.PCloudDrivePath;
				if (info is null || info.Label == "Google Drive" ||
					(!string.IsNullOrEmpty(pCloudDrivePath) && info.Root.Path.Equals(pCloudDrivePath, StringComparison.OrdinalIgnoreCase)))
					continue;
				yield return DriveItem.FromServer(info);
			}
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
