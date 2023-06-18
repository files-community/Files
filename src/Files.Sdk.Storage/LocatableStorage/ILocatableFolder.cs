// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Generic;

namespace Files.Sdk.Storage.LocatableStorage
{
	/// <summary>
	/// Represents a folder that resides within a folder structure.
	/// </summary>
	public interface ILocatableFolder : IFolder, ILocatableStorable
	{
		/// <summary>
		/// Search for items that match a set of user-supplied query terms
		/// </summary>
		/// <param name="userQuery">A string containing specific terms which influence search results</param>
		/// <returns>The search results produced by the query</returns>
		IAsyncEnumerable<IStorable> SearchAsync(string userQuery, SearchDepth depth = SearchDepth.Shallow);
	}

	public enum SearchDepth
	{
		Shallow,
		Deep
	}
}
