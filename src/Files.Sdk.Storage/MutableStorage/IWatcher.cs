// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.IO;

namespace Files.Sdk.Storage.MutableStorage
{
	/// <summary>
	/// Represents a filesystem watcher
	/// </summary>
	public interface IWatcher
	{
		/// <summary>
		/// Fires when an item addition is detected by the watcher
		/// </summary>
		event EventHandler<FileSystemEventArgs> ItemAdded;

		/// <summary>
		/// Fires when an item removal is detected by the watcher
		/// </summary>
		event EventHandler<FileSystemEventArgs> ItemRemoved;

		/// <summary>
		/// Starts the watcher
		/// </summary>
		void Start();

		/// <summary>
		/// Stops the watcher
		/// </summary>
		void Stop();
	}
}
