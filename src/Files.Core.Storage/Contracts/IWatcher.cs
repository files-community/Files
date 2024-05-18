// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage.Contracts
{
	/// <summary>
	/// A disposable object which can notify of changes to the folder.
	/// </summary>
	public interface IWatcher : IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Starts the watcher
		/// </summary>
		void StartWatcher();

		/// <summary>
		/// Stops the watcher
		/// </summary>
		void StopWatcher();
	}
}
