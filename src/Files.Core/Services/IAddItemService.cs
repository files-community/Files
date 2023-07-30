// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services
{
	/// <summary>
	/// A service to retrieve available item types
	/// </summary>
	public interface IAddItemService
	{
		/// <summary>
		/// Initialize the service
		/// </summary>
		/// <returns>Task</returns>
		Task InitializeAsync();

		/// <summary>
		/// Gets a list of the available item types
		/// </summary>
		/// <returns>List of the available item types</returns>
		List<ShellNewEntry> GetEntries();
	}
}
