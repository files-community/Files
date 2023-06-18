// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage.MutableStorage;
using System;
using System.IO;

namespace Files.Backend.Models
{
	/// <summary>
	/// Represents a storage device watcher
	/// </summary>
	public interface IStorageDeviceWatcher : IWatcher2
	{
		/// <summary>
		/// Fires when the storage device watcher completes an enumeration
		/// </summary>
		event EventHandler EnumerationCompleted;

		/// <summary>
		/// Fires when a device modification is detected by the storage device watcher
		/// </summary>
		event EventHandler<FileSystemEventArgs> ItemModified;

		/// <summary>
		/// Represents whether the storage device watcher should be started
		/// </summary>
		bool CanBeStarted { get; }
	}
}