// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Models;
using Files.Sdk.Storage.LocatableStorage;
using System;

namespace Files.Backend.Services
{
	/// <summary>
	/// 
	/// </summary>
	public interface ITrashService
	{
		/// <summary>
		/// Creates a watcher for trash bin
		/// </summary>
		/// <returns>The created trash bin watcher</returns>
		ITrashWatcher CreateWatcher();


	}
}