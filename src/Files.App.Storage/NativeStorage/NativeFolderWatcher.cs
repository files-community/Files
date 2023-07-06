// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.MutableStorage;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Storage.NativeStorage
{
	/// <inheritdoc cref="IFolderWatcher"/>
    public sealed class NativeFolderWatcher : IFolderWatcher
	{
		private FileSystemWatcher? _fileSystemWatcher;
		private NotifyCollectionChangedEventHandler? _collectionChanged;

		public IMutableFolder Folder { get; }

		/// <inheritdoc/>
		public event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add
			{
				if (_fileSystemWatcher is not null)
					_fileSystemWatcher.EnableRaisingEvents = true;

				_collectionChanged += value;
			}
			remove
			{
				if (_fileSystemWatcher is not null)
					_fileSystemWatcher.EnableRaisingEvents = false;

				_collectionChanged -= value;
			}
		}

		public NativeFolderWatcher(IMutableFolder folder)
		{
			Folder = folder;
			SetupWatcher();
		}

		private void SetupWatcher()
		{
			if (Folder is ILocatableFolder locatableFolder)
			{
				_fileSystemWatcher = new(locatableFolder.Path);
				_fileSystemWatcher.Created += FileSystemWatcher_Created;
				_fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
				_fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
			}
		}

		private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
		{
			_collectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Replace, e.FullPath, e.OldFullPath));
		}

		private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
		{
			_collectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, e.FullPath));
		}

		private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
		{
			_collectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, e.FullPath));
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
			if (_fileSystemWatcher is not null)
			{
				_fileSystemWatcher.EnableRaisingEvents = false;
				_fileSystemWatcher.Created -= FileSystemWatcher_Created;
				_fileSystemWatcher.Deleted -= FileSystemWatcher_Deleted;
				_fileSystemWatcher.Renamed -= FileSystemWatcher_Renamed;
				_fileSystemWatcher.Dispose();
			}
		}
	}
}
