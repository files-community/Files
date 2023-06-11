using Files.App.Shell;
using Files.App.Storage.NativeStorage;
using Files.Backend.Models;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using System.IO;

namespace Files.App.Filesystem
{
	public class RecycleBinWatcher : ITrashWatcher
	{
		private readonly NativeStorageService storageService;

		public event EventHandler<ILocatableStorable> RefreshRequested;
		public event EventHandler<ILocatableStorable> ItemAdded;
		public event EventHandler<ILocatableStorable> ItemRemoved;

		public RecycleBinWatcher(NativeStorageService storageService)
		{
			this.storageService = storageService;
		}

		private async void RecycleBinRefreshRequested(object sender, FileSystemEventArgs e)
		{
			if (e is null)
				return;

			RefreshRequested?.Invoke(sender, await storageService.GetStorableFromPathAsync(e.FullPath));
		}

		private async void RecycleBinItemDeleted(object sender, FileSystemEventArgs e)
		{
			if (e is null)
				return;

			ItemRemoved?.Invoke(sender, await storageService.GetStorableFromPathAsync(e.FullPath));
		}

		private async void RecycleBinItemCreated(object sender, FileSystemEventArgs e)
		{
			if (e is null)
				return;

			ItemAdded?.Invoke(sender, await storageService.GetStorableFromPathAsync(e.FullPath));
		}

		public void Start()
		{
			RecycleBinManager.Default.RecycleBinItemCreated += RecycleBinItemCreated;
			RecycleBinManager.Default.RecycleBinItemDeleted += RecycleBinItemDeleted;
			RecycleBinManager.Default.RecycleBinRefreshRequested += RecycleBinRefreshRequested;
		}

		public void Stop()
		{
			RecycleBinManager.Default.RecycleBinItemCreated -= RecycleBinItemCreated;
			RecycleBinManager.Default.RecycleBinItemDeleted -= RecycleBinItemDeleted;
			RecycleBinManager.Default.RecycleBinRefreshRequested -= RecycleBinRefreshRequested;
		}
	}
}
