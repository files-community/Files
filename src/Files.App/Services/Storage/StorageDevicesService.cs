// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Services.SizeProvider;
using Files.App.Storage.Storables;
using Files.App.Storage.Watchers;
using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Storage;

namespace Files.App.Services
{
	/// <inheritdoc cref="IRemovableDrivesService"/>
	public sealed class RemovableDrivesService : ObservableObject, IRemovableDrivesService
	{
		// Dependency injections

		private readonly ISizeProvider FolderSizeProvider = Ioc.Default.GetRequiredService<ISizeProvider>();
		private readonly ILogger<App> Logger = Ioc.Default.GetRequiredService<ILogger<App>>();

		// Fields

		private readonly DeviceWatcher _watcher;

		// Properties

		private ObservableCollection<ILocatableFolder> _Drives;
		/// <inheritdoc/>
		public ObservableCollection<ILocatableFolder> Drives
		{
			get => _Drives;
			private set => SetProperty(ref _Drives, value);
		}

		private bool _ShowUserConsentOnInit;
		/// <inheritdoc/>
		public bool ShowUserConsentOnInit
		{
			get => _ShowUserConsentOnInit;
			set => SetProperty(ref _ShowUserConsentOnInit, value);
		}

		// Constructor

		public RemovableDrivesService()
		{
			_Drives = [];

			_watcher = new();
			_watcher.DeviceAdded += Watcher_ItemAdded;
			_watcher.DeviceDeleted += Watcher_ItemDeleted;
			_watcher.DeviceInserted += Watcher_ItemChanged;
			_watcher.DeviceEjected += Watcher_ItemChanged;
			_watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
		}

		// Methods

		/// <inheritdoc/>
		public async Task UpdateDrivesAsync()
		{
			Drives.Clear();

			await foreach (ILocatableFolder item in GetDrivesAsync())
				Drives.AddIfNotPresent(item);

			var primaryDrive = await GetPrimaryDriveAsync();

			// Show consent dialog if the OS drive could not be accessed
			if (!Drives.Any(x => Path.GetFullPath(x.Path) == Path.GetFullPath(primaryDrive.Path)))
				ShowUserConsentOnInit = true;

			if (_watcher.CanBeStarted)
				_watcher.StartWatcher();
		}

		private async IAsyncEnumerable<ILocatableFolder> GetDrivesAsync()
		{
			var list = DriveInfo.GetDrives();
			var pCloudDrivePath = App.AppModel.PCloudDrivePath;

			var sw = Stopwatch.StartNew();
			var googleDrivePath = GoogleDriveCloudDetector.GetRegistryBasePath();
			sw.Stop();
			Debug.WriteLine($"In RemovableDrivesService: Time elapsed for registry check: {sw.Elapsed}");
			App.AppModel.GoogleDrivePath = googleDrivePath ?? string.Empty;

			foreach (var drive in list)
			{
				// We don't want cloud drives to appear in a plain "Drives" section.
				if (drive.Name.Equals(googleDrivePath) || drive.Name.Equals(pCloudDrivePath))
					continue;

				var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Name).AsTask());
				if (res.ErrorCode is FileSystemStatusCode.Unauthorized)
				{
					App.Logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name}, failed at the StorageFolder initialization step. This device will be ignored.");
					continue;
				}
				else if (!res)
				{
					App.Logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name}, failed at the StorageFolder initialization step. This device will be ignored.");
					continue;
				}

				using var thumbnail = await DriveHelpers.GetThumbnailAsync(res.Result);
				var type = DriveHelpers.GetDriveType(drive);
				var label = DriveHelpers.GetExtendedDriveLabel(drive);
				var driveItem = await DriveItem.CreateFromPropertiesAsync(res.Result, drive.Name.TrimEnd('\\'), label, type, thumbnail);

				App.Logger.LogInformation($"Drive added: {driveItem.Path}, {driveItem.Type}");

				yield return driveItem;
			}
		}

		private async Task<ILocatableFolder> GetPrimaryDriveAsync()
		{
			string cDrivePath = $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\";
			return new WindowsStorageFolder(await StorageFolder.GetFolderFromPathAsync(cDrivePath));
		}

		private async Task UpdateDrivePropertiesAsync(ILocatableFolder drive)
		{
			var rootModified = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Path).AsTask());
			if (rootModified && drive is DriveItem matchingDriveEjected)
			{
				_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					matchingDriveEjected.Root = rootModified.Result;
					matchingDriveEjected.Text = rootModified.Result.DisplayName;
					return matchingDriveEjected.UpdatePropertiesAsync();
				});
			}
		}

		// Event methods

		private async void Watcher_ItemAdded(object? sender, DeviceEventArgs e)
		{
			var driveAdded = new DriveInfo(e.DeviceId);

			var rootAdded = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(e.DeviceId).AsTask());
			if (!rootAdded)
			{
				App.Logger.LogWarning($"{rootAdded.ErrorCode}: Attempting to add the device, {e.DeviceId}, failed at the StorageFolder initialization step. This device will be ignored.");
				return;
			}

			var type = DriveHelpers.GetDriveType(driveAdded);
			var label = DriveHelpers.GetExtendedDriveLabel(driveAdded);
			DriveItem driveItem = await DriveItem.CreateFromPropertiesAsync(rootAdded, e.DeviceId, label, type);

			lock (Drives)
			{
				// If drive already in list, remove it first.
				var matchingDrive = Drives.FirstOrDefault(x =>
					x.Id == driveItem.Id ||
					string.IsNullOrEmpty(driveItem.Path)
						? x.Path.Contains(driveItem.Name, StringComparison.OrdinalIgnoreCase)
						: Path.GetFullPath(x.Path) == Path.GetFullPath(driveItem.Path));

				if (matchingDrive is not null)
					Drives.Remove(matchingDrive);

				Drives.Add(driveItem);
			}

			Logger.LogInformation($"Drive added: {driveItem.Path}");

			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		private void Watcher_ItemDeleted(object? sender, DeviceEventArgs e)
		{
			lock (Drives)
			{
				var drive = Drives.FirstOrDefault(x => x.Id == e.DeviceId);
				if (drive is not null)
					Drives.Remove(drive);
			}

			Logger.LogInformation($"Drive removed: {e}");

			// Update the collection on the ui-thread.
			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		private async void Watcher_ItemChanged(object? sender, DeviceEventArgs e)
		{
			var matchingDriveEjected = Drives.FirstOrDefault(x => Path.GetFullPath(x.Path) == Path.GetFullPath(e.DeviceId));
			if (matchingDriveEjected != null)
				await UpdateDrivePropertiesAsync(matchingDriveEjected);
		}

		private async void Watcher_EnumerationCompleted(object? sender, EventArgs e)
		{
			Logger.LogDebug("Watcher_EnumerationCompleted");
			await FolderSizeProvider.CleanAsync();
		}
	}
}
