// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;

namespace Files.App.Data.Contracts
{
	public interface IWindowsLibraryService
	{
		event EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		IReadOnlyList<LibraryLocationItem> Libraries { get; }

		/// <summary>
		/// Get libraries of the current user with the help of the FullTrust process.
		/// </summary>
		/// <returns>List of library items</returns>
		Task<List<LibraryLocationItem>> GetLibrariesAsync();

		Task UpdateLibrariesAsync();

		/// <summary>
		/// Update library details.
		/// </summary>
		/// <param name="libraryFilePath">Library file path</param>
		/// <param name="defaultSaveFolder">Update the default save folder or null to keep current</param>
		/// <param name="folders">Update the library folders or null to keep current</param>
		/// <param name="isPinned">Update the library pinned status or null to keep current</param>
		/// <returns>The new library if successfully updated</returns>
		Task<LibraryLocationItem> UpdateLibraryAsync(string libraryPath, string? defaultSaveFolder = null, string[]? folders = null, bool? isPinned = null);

		bool TryGetLibrary(string path, out LibraryLocationItem library);

		/// <summary>
		/// Create a new library with the specified name.
		/// </summary>
		/// <param name="name">The name of the new library (must be unique)</param>
		/// <returns>The new library if successfully created</returns>
		Task<bool> CreateNewLibrary(string name);

		(bool result, string reason) CanCreateLibrary(string name);

		bool IsLibraryPath(string path);
	}
}
