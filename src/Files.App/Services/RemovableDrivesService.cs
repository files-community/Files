// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Services.SizeProvider;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.Devices.Portable;
using Windows.Storage;

namespace Files.App.Services
{
	/// <inheritdoc cref="IRemovableDrivesService"/>
	public class RemovableDrivesService : IRemovableDrivesService
	{
		// Dependency injections

		private ISizeProvider FolderSizeProvider { get; } = Ioc.Default.GetRequiredService<ISizeProvider>();
		private ILogger<App> Logger { get; } = Ioc.Default.GetRequiredService<ILogger<App>>();

		// Fields

		private IDeviceWatcher? _watcher;

		// Properties

		private ObservableCollection<ILocatableFolder> _Drives = [];
		/// <inheritdoc/>
		public ObservableCollection<ILocatableFolder> RemovableDrives
		{
			get => _Drives;
			private set => NotifyPropertyChanged(nameof(RemovableDrives));
		}

		private bool _ShowUserConsentOnInit;
		/// <inheritdoc/>
		public bool ShowUserConsentOnInit
		{
			get => _ShowUserConsentOnInit;
			set => NotifyPropertyChanged(nameof(ShowUserConsentOnInit));
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		// Methods

		/// <inheritdoc/>
		public void InitializeRemovableDrivesWatcher()
		{
			_watcher ??= new StorageDeviceWatcher();
			_watcher.ItemAdded += Watcher_DeviceAdded;
			_watcher.ItemDeleted += Watcher_DeviceRemoved;
			_watcher.ItemChanged += Watcher_DeviceChanged;
			_watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
		}

		/// <inheritdoc/>
		public async Task RefreshRemovableDrivesAsync()
		{
			RemovableDrives.Clear();

			await foreach (ILocatableFolder item in GetDrivesAsync())
				RemovableDrives.AddIfNotPresent(item);

			var osDrive = await GetPrimaryDriveAsync();

			// Show consent dialog if the OS drive could not be accessed
			if (!RemovableDrives.Any(x => Path.GetFullPath(x.Path) == Path.GetFullPath(osDrive.Path)))
				ShowUserConsentOnInit = true;

			if (_watcher?.CanBeStarted ?? false)
				_watcher?.StartWatcher();
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<ILocatableFolder> GetDrivesAsync()
		{
			var list = DriveInfo.GetDrives();
			var googleDrivePath = App.AppModel.GoogleDrivePath;
			var pCloudDrivePath = App.AppModel.PCloudDrivePath;

			foreach (var drive in list)
			{
				var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Name).AsTask());
				if (res.ErrorCode is FileSystemStatusCode.Unauthorized)
				{
					App.Logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name},"
						+ " failed at the StorageFolder initialization step. This device will be ignored.");
					continue;
				}
				else if (!res)
				{
					App.Logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name},"
						+ " failed at the StorageFolder initialization step. This device will be ignored.");
					continue;
				}

				using var thumbnail = await DriveHelpers.GetThumbnailAsync(res.Result);
				var type = DriveHelpers.GetDriveType(drive);
				var label = DriveHelpers.GetExtendedDriveLabel(drive);
				var driveItem = await DriveItem.CreateFromPropertiesAsync(res.Result, drive.Name.TrimEnd('\\'), label, type, thumbnail);

				// Don't add here because Google Drive is already displayed under cloud drives
				if (drive.Name == googleDrivePath || drive.Name == pCloudDrivePath)
					continue;

				App.Logger.LogInformation($"Drive added: {driveItem.Path}, {driveItem.Type}");

				yield return driveItem;
			}
		}

		/// <inheritdoc/>
		public async Task<ILocatableFolder> GetPrimaryDriveAsync()
		{
			string cDrivePath = @"C:\";
			return new WindowsStorageFolder(await StorageFolder.GetFolderFromPathAsync(cDrivePath));
		}

		/// <inheritdoc/>
		public async Task UpdateDrivePropertiesAsync(ILocatableFolder drive)
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

		// Event Methods

		private async void Watcher_DeviceAdded(object? sender, DeviceEventArgs args)
		{
			var root = StorageDevice.FromId(args.DeviceId);
			var driveAdded = new DriveInfo(args.DeviceId);
			var rootAdded = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(args.DeviceId).AsTask());

			var type = DriveHelpers.GetDriveType(driveAdded);
			var label = DriveHelpers.GetExtendedDriveLabel(driveAdded);

			DriveItem driveItem = await DriveItem.CreateFromPropertiesAsync(rootAdded, args.DeviceId, label, type);

			lock (RemovableDrives)
			{
				// If drive already in list, remove it first.
				var matchingDrive = RemovableDrives.FirstOrDefault(x =>
					x.Id == args.DeviceId ||
					string.IsNullOrEmpty(root.Path)
						? x.Path.Contains(root.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase)
						: Path.GetFullPath(x.Path) == Path.GetFullPath(root.Path ?? string.Empty)
				);

				if (matchingDrive is not null)
					RemovableDrives.Remove(matchingDrive);

				RemovableDrives.Add(driveItem);
			}

			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		private void Watcher_DeviceRemoved(object? sender, DeviceEventArgs args)
		{
			lock (RemovableDrives)
			{
				var drive = RemovableDrives.FirstOrDefault(x => x.Id == args.DeviceId);
				if (drive is not null)
					RemovableDrives.Remove(drive);
			}

			// Update the collection on the ui-thread.
			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		private async void Watcher_DeviceChanged(object? sender, DeviceEventArgs args)
		{
			var root = StorageDevice.FromId(args.DeviceId);

			var matchingDriveEjected = RemovableDrives.FirstOrDefault(x => Path.GetFullPath(x.Path) == Path.GetFullPath(root.Path ?? string.Empty));
			if (matchingDriveEjected != null)
				await UpdateDrivePropertiesAsync(matchingDriveEjected);
		}

		private async void Watcher_EnumerationCompleted(object? sender, EventArgs args)
		{
			await FolderSizeProvider.CleanAsync();
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Disposer

		public void Dispose()
		{
			if (_watcher is null)
				return;

			_watcher.StopsWatcher();
			_watcher.ItemAdded -= Watcher_DeviceAdded;
			_watcher.ItemDeleted -= Watcher_DeviceRemoved;
			_watcher.ItemChanged -= Watcher_DeviceChanged;
			_watcher.EnumerationCompleted -= Watcher_EnumerationCompleted;
		}
	}
}
