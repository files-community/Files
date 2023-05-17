// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Models;
using Files.Backend.Services;
using Files.Backend.Services.SizeProvider;
using Files.Sdk.Storage.LocatableStorage;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Files.App.Data.Models
{
	public class DrivesViewModel : ObservableObject, IDisposable
	{
		private readonly IRemovableDrivesService _removableDrivesService;

		private readonly ISizeProvider _folderSizeProvider;

		private readonly IStorageDeviceWatcher _watcher;

		private readonly ILogger<App> _logger;

		private bool showUserConsentOnInit;
		public bool ShowUserConsentOnInit
		{
			get => showUserConsentOnInit;
			set => SetProperty(ref showUserConsentOnInit, value);
		}

		private ObservableCollection<ILocatableFolder> _drives;
		public ObservableCollection<ILocatableFolder> Drives
		{
			get => _drives;
			private set => SetProperty(ref _drives, value);
		}

		public DrivesViewModel(IRemovableDrivesService removableDrivesService, ISizeProvider folderSizeProvider, ILogger<App> logger)
		{
			_removableDrivesService = removableDrivesService;
			_folderSizeProvider = folderSizeProvider;
			_logger = logger;

			_drives = new();

			_watcher = removableDrivesService.CreateWatcher();
			_watcher.DeviceAdded += Watcher_DeviceAdded;
			_watcher.DeviceRemoved += Watcher_DeviceRemoved;
			_watcher.DeviceModified += Watcher_DeviceModified;
			_watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
		}

		private async void Watcher_EnumerationCompleted(object? sender, EventArgs e)
		{
			_logger.LogDebug("Watcher_EnumerationCompleted");

			await _folderSizeProvider.CleanAsync();
		}

		private async void Watcher_DeviceModified(object? sender, string e)
		{
			var matchingDriveEjected = Drives.FirstOrDefault(x => Path.GetFullPath(x.Path) == Path.GetFullPath(e));
			if (matchingDriveEjected != null)
				await _removableDrivesService.UpdateDrivePropertiesAsync(matchingDriveEjected);
		}

		private void Watcher_DeviceRemoved(object? sender, string e)
		{
			_logger.LogInformation($"Drive removed: {e}");

			lock (Drives)
			{
				var drive = Drives.FirstOrDefault(x => x.Id == e);
				if (drive is not null)
					Drives.Remove(drive);
			}

			// Update the collection on the ui-thread.
			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		private void Watcher_DeviceAdded(object? sender, ILocatableFolder e)
		{
			lock (Drives)
			{
				// If drive already in list, remove it first.
				var matchingDrive = Drives.FirstOrDefault(x =>
					x.Id == e.Id ||
					string.IsNullOrEmpty(e.Path)
						? x.Path.Contains(e.Name, StringComparison.OrdinalIgnoreCase)
						: Path.GetFullPath(x.Path) == Path.GetFullPath(e.Path)
				);

				if (matchingDrive is not null)
					Drives.Remove(matchingDrive);

				_logger.LogInformation($"Drive added: {e.Path}");
				Drives.Add(e);
			}

			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		public async Task UpdateDrivesAsync()
		{
			Drives.Clear();

			await foreach (ILocatableFolder item in _removableDrivesService.GetDrivesAsync())
				Drives.AddIfNotPresent(item);

			var osDrive = await _removableDrivesService.GetPrimaryDriveAsync();

			// Show consent dialog, if the OS drive could not be accessed
			if (!Drives.Any(x => Path.GetFullPath(x.Path) == Path.GetFullPath(osDrive.Path)))
				ShowUserConsentOnInit = true;

			if (_watcher.CanBeStarted)
				_watcher.Start();
		}

		public void Dispose()
		{
			_watcher.Stop();
			_watcher.DeviceAdded -= Watcher_DeviceAdded;
			_watcher.DeviceRemoved -= Watcher_DeviceRemoved;
			_watcher.DeviceModified -= Watcher_DeviceModified;
			_watcher.EnumerationCompleted -= Watcher_EnumerationCompleted;
		}
	}
}
