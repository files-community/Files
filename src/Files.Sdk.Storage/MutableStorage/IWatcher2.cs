using System;
using System.IO;

namespace Files.Sdk.Storage.MutableStorage
{
	public interface IWatcher2 : IWatcher
	{
		/// <summary>
		/// Fires when an item addition is detected by the watcher
		/// </summary>
		event EventHandler<FileSystemEventArgs> ItemAdded;

		/// <summary>
		/// Fires when an item removal is detected by the watcher
		/// </summary>
		event EventHandler<FileSystemEventArgs> ItemRemoved;
	}
}
