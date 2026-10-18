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
			watcher.Changed += Watcher_Changed;
		}

		private async void Watcher_Changed(object? sender, EventArgs e)
		{
			await SafetyExtensions.IgnoreExceptions(async () =>
			{
				await UpdateDrivesAsync();
				await folderSizeProvider.CleanAsync();
			}, logger);
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
			watcher.Changed -= Watcher_Changed;
		}
	}
}
