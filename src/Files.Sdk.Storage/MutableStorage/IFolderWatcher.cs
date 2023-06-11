// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Collections.Specialized;
using System.IO;

namespace Files.Sdk.Storage.MutableStorage
{
	/// <summary>
	/// A disposable object which can notify of changes to the folder.
	/// </summary>
	public interface IFolderWatcher : INotifyCollectionChanged, IDisposable, IAsyncDisposable, IWatcher
	{
		/// <summary>
		/// The folder being watched for changes.
		/// </summary>
		public IMutableFolder Folder { get; }

		/// <summary>
		/// Fires when an item change is detected by the watcher
		/// </summary>
		event EventHandler<FileSystemEventArgs> ItemChanged;

		/// <summary>
		/// Fires when an item change is detected by the watcher
		/// </summary>
		event EventHandler<RenamedEventArgs> ItemRenamed;
	}
}
