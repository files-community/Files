// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;
using System.Security.Principal;

namespace Files.App.Storage
{
	public class RecycleBinWatcher : IWatcher, ITrashWatcher
	{
		private List<SystemIO.FileSystemWatcher>? _fileSystemWatchers;

		/// <inheritdoc/>
		public IMutableFolder TargetFolder { get; }

		public event EventHandler<SystemIO.FileSystemEventArgs>? ItemAdded;
		public event EventHandler<SystemIO.FileSystemEventArgs>? ItemDeleted;
		public event EventHandler<SystemIO.FileSystemEventArgs>? ItemChanged;
		public event EventHandler<SystemIO.FileSystemEventArgs>? ItemRenamed;
		public event EventHandler<SystemIO.FileSystemEventArgs>? RefreshRequested;

		private NotifyCollectionChangedEventHandler? _CollectionChanged;
		/// <inheritdoc/>
		public event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add
			{
				if (_fileSystemWatchers is not null)
				{
					foreach(var watcher in _fileSystemWatchers)
						watcher.EnableRaisingEvents = true;
				}

				_CollectionChanged += value;
			}
			remove
			{
				if (_fileSystemWatchers is not null)
				{
					foreach (var watcher in _fileSystemWatchers)
						watcher.EnableRaisingEvents = false;
				}

				_CollectionChanged -= value;
			}
		}

		public RecycleBinWatcher(IMutableFolder folder)
		{
			TargetFolder = folder;
			StartWatcher();
		}

		/// <inheritdoc/>
		public void StartWatcher()
		{
			if (TargetFolder is ILocatableFolder locatableFolder)
			{
				// NOTE: SHChangeNotifyRegister only works if recycle bin is opened in File Explorer
				_fileSystemWatchers = [];

				var sid = WindowsIdentity.GetCurrent().User!.ToString();

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

					_fileSystemWatchers.Add(watcher);
				}
			}
		}

		/// <inheritdoc/>
		public void StopsWatcher()
		{
			if (_fileSystemWatchers is not null)
			{
				foreach (var watcher in _fileSystemWatchers)
					watcher.Dispose();
			}
		}

		private void RecycleBinWatcher_Changed(object sender, SystemIO.FileSystemEventArgs e)
		{
			Debug.WriteLine($"Recycle bin event: {e.ChangeType}, {e.FullPath}");

			// Recycle bin also stores a file starting with $I for each item
			if (!string.IsNullOrEmpty(e.Name) && e.Name.StartsWith("$I", StringComparison.Ordinal))
				return;

			switch (e.ChangeType)
			{
				case SystemIO.WatcherChangeTypes.Created:
					ItemAdded?.Invoke(this, e);
					break;
				case SystemIO.WatcherChangeTypes.Deleted:
					ItemDeleted?.Invoke(this, e);
					break;
				case SystemIO.WatcherChangeTypes.Renamed:
					ItemRenamed?.Invoke(this, e);
					break;
				default:
					RefreshRequested?.Invoke(this, e);
					break;
			}
		}

		/// <inheritdoc/>
		public ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			StopsWatcher();
		}
	}
}
