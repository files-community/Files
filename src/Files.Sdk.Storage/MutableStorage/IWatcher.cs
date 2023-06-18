// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Sdk.Storage.MutableStorage
{
	/// <summary>
	/// Represents a filesystem watcher
	/// </summary>
	public interface IWatcher
	{
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
