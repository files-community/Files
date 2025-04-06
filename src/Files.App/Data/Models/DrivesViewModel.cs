// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Services.SizeProvider;
using Files.Core.Storage.Storables;
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
			var matchingDriveEjected = Drives.FirstOrDefault(x => Path.GetFullPath(x.Id) == Path.GetFullPath(e));
			if (matchingDriveEjected != null)
				await removableDrivesService.UpdateDrivePropertiesAsync(matchingDriveEjected);
		}

		private void Watcher_DeviceRemoved(object? sender, string e)
		{
			logger.LogInformation($"Drive removed: {e}");
			lock (Drives)
			{
				var drive = Drives.FirstOrDefault(x => (x as DriveItem)?.DeviceID == e);
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
					string.IsNullOrEmpty(e.Id)
						? x.Id.Contains(e.Name, StringComparison.OrdinalIgnoreCase)
						: Path.GetFullPath(x.Id) == Path.GetFullPath(e.Id)
				);

				if (matchingDrive is not null)
					Drives.Remove(matchingDrive);

				logger.LogInformation($"Drive added: {e.Id}");
				Drives.Add(e);
			}

			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		public async Task UpdateDrivesAsync()
		{
			Drives.Clear();
			await foreach (IFolder item in removableDrivesService.GetDrivesAsync())
			{
				Drives.AddIfNotPresent(item);
			}

			var osDrive = await removableDrivesService.GetPrimaryDriveAsync();
			var osDrivePath = osDrive.Id.EndsWith(Path.DirectorySeparatorChar)
				? osDrive.Id
				: $"{osDrive.Id}{Path.DirectorySeparatorChar}";

			// Show consent dialog if the OS drive could not be accessed
			if (Drives.All(x => Path.GetFullPath(x.Id) != osDrivePath))
				ShowUserConsentOnInit = true;

			if (watcher.CanBeStarted)
				watcher.Start();
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
