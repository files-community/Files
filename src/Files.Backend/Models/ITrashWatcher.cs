// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.MutableStorage;
using System;

namespace Files.Backend.Models
{
	/// <summary>
	/// Represents a watcher for the trash bin
	/// </summary>
	public interface ITrashWatcher : IWatcher
	{
		/// <summary>
		/// Fires when a refresh is needed
		/// </summary>
		event EventHandler<ILocatableStorable> RefreshRequested;
	}
}
