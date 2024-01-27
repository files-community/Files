// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage
{
	public interface ITrashWatcher
	{
		/// <summary>
		/// Gets invoked when an refresh request is detected by the watcher
		/// </summary>
		event EventHandler<SystemIO.FileSystemEventArgs>? RefreshRequested;
	}
}
