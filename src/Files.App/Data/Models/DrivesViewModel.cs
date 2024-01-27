// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Services.SizeProvider;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Files.App.Data.Models
{
	public class DrivesViewModel : ObservableObject, IDisposable
	{
		private IRemovableDrivesService RemovableDrivesService { get; } = Ioc.Default.GetRequiredService<IRemovableDrivesService>();
		private ISizeProvider FolderSizeProvider { get; } = Ioc.Default.GetRequiredService<ISizeProvider>();
		private ILogger<App> Logger { get; } = Ioc.Default.GetRequiredService<ILogger<App>>();

		private readonly IStorageDeviceWatcher _watcher;

		private ObservableCollection<ILocatableFolder> _Drives = [];
		public ObservableCollection<ILocatableFolder> Drives
		{
			get => _Drives;
			private set => SetProperty(ref _Drives, value);
		}

		private bool _ShowUserConsentOnInit;
		public bool ShowUserConsentOnInit
		{
			get => _ShowUserConsentOnInit;
			set => SetProperty(ref _ShowUserConsentOnInit, value);
		}

		public DrivesViewModel()
		{
			_watcher = RemovableDrivesService.CreateWatcher();
			_watcher.DeviceAdded += Watcher_DeviceAdded;
			_watcher.DeviceRemoved += Watcher_DeviceRemoved;
			_watcher.DeviceModified += Watcher_DeviceModified;
			_watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
		}

		private async void Watcher_EnumerationCompleted(object? sender, System.EventArgs e)
		{
			Logger.LogDebug("Watcher_EnumerationCompleted");
			await FolderSizeProvider.CleanAsync();
		}

		private async void Watcher_DeviceModified(object? sender, string e)
		{
			var matchingDriveEjected = Drives.FirstOrDefault(x => Path.GetFullPath(x.Path) == Path.GetFullPath(e));
			if (matchingDriveEjected != null)
				await RemovableDrivesService.UpdateDrivePropertiesAsync(matchingDriveEjected);
		}

		private void Watcher_DeviceRemoved(object? sender, string e)
		{
			Logger.LogInformation($"Drive removed: {e}");
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

				Logger.LogInformation($"Drive added: {e.Path}");
				Drives.Add(e);
			}

			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		public async Task UpdateDrivesAsync()
		{
			Drives.Clear();
			await foreach (ILocatableFolder item in RemovableDrivesService.GetDrivesAsync())
			{
				Drives.AddIfNotPresent(item);
			}

			var osDrive = await RemovableDrivesService.GetPrimaryDriveAsync();

			// Show consent dialog if the OS drive could not be accessed
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
