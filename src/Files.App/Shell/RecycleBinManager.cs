// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Security.Principal;

namespace Files.App.Shell
{
	public sealed class RecycleBinManager
	{
		private static readonly Lazy<RecycleBinManager> lazy = new(() => new RecycleBinManager());

		private IList<SystemIO.FileSystemWatcher>? binWatchers;

		public event SystemIO.FileSystemEventHandler? RecycleBinItemCreated;

		public event SystemIO.FileSystemEventHandler? RecycleBinItemDeleted;

		public event SystemIO.FileSystemEventHandler? RecycleBinItemRenamed;

		public event SystemIO.FileSystemEventHandler? RecycleBinRefreshRequested;

		public static RecycleBinManager Default
			=> lazy.Value;

		private RecycleBinManager()
		{
			Initialize();
		}

		private void Initialize()
		{
			// Create shell COM object and get recycle bin folder
			StartRecycleBinWatcher();
		}

		private void StartRecycleBinWatcher()
		{
			// NOTE: SHChangeNotifyRegister only works if recycle bin is open in explorer
			// Create file system watcher to monitor recycle bin folder(s)
			binWatchers = new List<SystemIO.FileSystemWatcher>();

			var sid = WindowsIdentity.GetCurrent().User.ToString();

			foreach (var drive in SystemIO.DriveInfo.GetDrives())
			{
				var recyclePath = SystemIO.Path.Combine(drive.Name, "$RECYCLE.BIN", sid);

				if (drive.DriveType == SystemIO.DriveType.Network || !SystemIO.Directory.Exists(recyclePath))
					continue;

				SystemIO.FileSystemWatcher watcher = new()
				{
					Path = recyclePath,
					Filter = "*.*",
					NotifyFilter = SystemIO.NotifyFilters.LastWrite | SystemIO.NotifyFilters.FileName | SystemIO.NotifyFilters.DirectoryName
				};

				watcher.Created += RecycleBinWatcher_Changed;
				watcher.Deleted += RecycleBinWatcher_Changed;
				watcher.EnableRaisingEvents = true;

				binWatchers.Add(watcher);
			}
		}

		private void RecycleBinWatcher_Changed(object sender, SystemIO.FileSystemEventArgs e)
		{
			Debug.WriteLine($"Recycle bin event: {e.ChangeType}, {e.FullPath}");

			if (e.Name.StartsWith("$I", StringComparison.Ordinal))
			{
				// Recycle bin also stores a file starting with $I for each item
				return;
			}

			switch (e.ChangeType)
			{
				case SystemIO.WatcherChangeTypes.Created:
					RecycleBinItemCreated?.Invoke(this, e);
					break;
				case SystemIO.WatcherChangeTypes.Deleted:
					RecycleBinItemDeleted?.Invoke(this, e);
					break;
				case SystemIO.WatcherChangeTypes.Renamed:
					RecycleBinItemRenamed?.Invoke(this, e);
					break;
				default:
					RecycleBinRefreshRequested?.Invoke(this, e);
					break;
			}
		}

		private void Unregister()
		{
			if (binWatchers is not null)
			{
				foreach (var watcher in binWatchers)
					watcher.Dispose();
			}
		}

		~RecycleBinManager()
		{
			Unregister();
		}
	}
}
