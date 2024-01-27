// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;

namespace Files.App.Storage
{
	/// <inheritdoc cref="IWatcher"/>
	public sealed class NativeFolderWatcher : IWatcher
	{
		private SystemIO.FileSystemWatcher? _fileSystemWatcher;

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
				if (_fileSystemWatcher is not null)
					_fileSystemWatcher.EnableRaisingEvents = true;

				_CollectionChanged += value;
			}
			remove
			{
				if (_fileSystemWatcher is not null)
					_fileSystemWatcher.EnableRaisingEvents = false;

				_CollectionChanged -= value;
			}
		}

		public NativeFolderWatcher(IMutableFolder folder)
		{
			TargetFolder = folder;
			StartWatcher();
		}

		/// <inheritdoc/>
		public void StartWatcher()
		{
			if (TargetFolder is ILocatableFolder locatableFolder)
			{
				_fileSystemWatcher = new(locatableFolder.Path);
				_fileSystemWatcher.Created += FileSystemWatcher_Changed;
				_fileSystemWatcher.Deleted += FileSystemWatcher_Changed;
				_fileSystemWatcher.Renamed += FileSystemWatcher_Changed;
			}
		}

		/// <inheritdoc/>
		public void StopsWatcher()
		{
			if (_fileSystemWatcher is not null)
			{
				_fileSystemWatcher.EnableRaisingEvents = false;
				_fileSystemWatcher.Created -= FileSystemWatcher_Changed;
				_fileSystemWatcher.Deleted -= FileSystemWatcher_Changed;
				_fileSystemWatcher.Renamed -= FileSystemWatcher_Changed;
				_fileSystemWatcher.Dispose();
			}
		}

		private void FileSystemWatcher_Changed(object sender, SystemIO.FileSystemEventArgs e)
		{
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
