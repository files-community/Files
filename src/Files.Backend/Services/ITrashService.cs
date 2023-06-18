// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Models;

namespace Files.Backend.Services
{
	public interface ITrashService
	{
		/// <summary>
		/// Creates a watcher for trash bin
		/// </summary>
		/// <returns>The created trash bin watcher</returns>
		ITrashWatcher CreateWatcher();
	}
}