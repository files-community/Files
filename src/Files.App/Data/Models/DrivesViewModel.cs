// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Services.SizeProvider;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Files.App.Data.Models
{
	public sealed partial class DrivesViewModel : ObservableObject, IDisposable
	{
		public ObservableCollection<IFolder> Drives
		{
			get => drives;
			private set => SetProperty(ref drives, value);
		}

		public bool ShowUserConsentOnInit
		{
			get => showUserConsentOnInit;
			set => SetProperty(ref showUserConsentOnInit, value);
		}

		private bool showUserConsentOnInit;
		private ObservableCollection<IFolder> drives;
		private readonly SemaphoreSlim updateDrivesGate = new(1, 1);
		private readonly IRemovableDrivesService removableDrivesService;
		private readonly ISizeProvider folderSizeProvider;
		private readonly IStorageDeviceWatcher watcher;
		private readonly ILogger<App> logger;

		public DrivesViewModel(IRemovableDrivesService removableDrivesService, ISizeProvider folderSizeProvider, ILogger<App> logger)
		{
			this.removableDrivesService = removableDrivesService;
			this.folderSizeProvider = folderSizeProvider;
			this.logger = logger;

			drives = [];

			watcher = removableDrivesService.CreateWatcher();
			watcher.DeviceAdded += Watcher_DeviceAdded;
			watcher.DeviceRemoved += Watcher_DeviceRemoved;
			watcher.DeviceModified += Watcher_DeviceModified;
			watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
		}

		private async void Watcher_EnumerationCompleted(object? sender, System.EventArgs e)
		{
			logger.LogDebug("Watcher_EnumerationCompleted");
			await folderSizeProvider.CleanAsync();
		}

		private async void Watcher_DeviceModified(object? sender, string e)
		{
			var matchingDriveEjected = Drives.FirstOrDefault(x => Path.GetFullPath(x.Id).Equals(Path.GetFullPath(e), StringComparison.OrdinalIgnoreCase));
			if (matchingDriveEjected != null)
				await removableDrivesService.UpdateDrivePropertiesAsync(matchingDriveEjected);
		}

		private void Watcher_DeviceRemoved(object? sender, string e)
		{
			logger.LogInformation($"Drive removed: {e}");
			lock (Drives)
			{
				// Depending on the event source, drives are identified by a drive letter
				// or a device interface ID, so match on either.
				var drive = Drives.FirstOrDefault(x =>
					(x as DriveItem)?.DeviceID == e ||
					x.Id.TrimEnd('\\').Equals(e.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
				if (drive is not null)
					Drives.Remove(drive);
			}

			// Update the collection on the ui-thread.
			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		private void Watcher_DeviceAdded(object? sender, IFolder e)
		{
			lock (Drives)
			{
				// If drive already in list, remove it first.
				var matchingDrive = Drives.FirstOrDefault(x =>
					(x as DriveItem)?.DeviceID == (e as DriveItem)?.DeviceID ||
					(string.IsNullOrEmpty(e.Id)
						? x.Id.Contains(e.Name, StringComparison.OrdinalIgnoreCase)
						: Path.GetFullPath(x.Id).Equals(Path.GetFullPath(e.Id), StringComparison.OrdinalIgnoreCase))
				);

				if (matchingDrive is not null)
					Drives.Remove(matchingDrive);

				logger.LogInformation($"Drive added: {e.Id}");
				InsertSorted(e);
			}

			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		public async Task UpdateDrivesAsync()
		{
			await updateDrivesGate.WaitAsync();

			try
			{
				lock (Drives)
					Drives.Clear();

				await foreach (IFolder item in removableDrivesService.GetDrivesAsync())
				{
					lock (Drives)
						InsertSorted(item);
				}

				var osDrive = await removableDrivesService.GetPrimaryDriveAsync();

				// Show consent dialog if the OS drive could not be accessed
				if (osDrive is null)
				{
					ShowUserConsentOnInit = true;
				}
				else
				{
					var osDrivePath = osDrive.Id.EndsWith(Path.DirectorySeparatorChar)
						? osDrive.Id
						: $"{osDrive.Id}{Path.DirectorySeparatorChar}";

					bool isOsDriveMissing;
					lock (Drives)
						isOsDriveMissing = Drives.All(x => string.IsNullOrEmpty(x.Id) || !Path.GetFullPath(x.Id).Equals(osDrivePath, StringComparison.OrdinalIgnoreCase));

					if (isOsDriveMissing)
						ShowUserConsentOnInit = true;
				}

				if (watcher.CanBeStarted)
					watcher.Start();
			}
			finally
			{
				updateDrivesGate.Release();
			}
		}

		// Callers must hold the Drives lock
		private void InsertSorted(IFolder item)
		{
			if (string.IsNullOrEmpty(item.Id))
			{
				Drives.Add(item);
				return;
			}

			var path = Path.GetFullPath(item.Id);
			var index = 0;

			foreach (var drive in Drives)
			{
				var comparison = string.IsNullOrEmpty(drive.Id)
					? 1
					: string.Compare(Path.GetFullPath(drive.Id), path, StringComparison.OrdinalIgnoreCase);

				if (comparison == 0)
					return;
				if (comparison > 0)
					break;

				index++;
			}

			Drives.Insert(index, item);
		}

		public void Dispose()
		{
			watcher.Stop();
			watcher.DeviceAdded -= Watcher_DeviceAdded;
			watcher.DeviceRemoved -= Watcher_DeviceRemoved;
			watcher.DeviceModified -= Watcher_DeviceModified;
			watcher.EnumerationCompleted -= Watcher_EnumerationCompleted;
		}
	}
}
