// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
	/// <summary>
	/// A service to retrieve available item types
	/// </summary>
	public interface IAddItemService
	{
		/// <summary>
		/// Gets a list of the available item types
		/// </summary>
		/// <returns>List of the available item types</returns>
		Task<List<ShellNewEntry>> GetNewEntriesAsync();
	}
}
