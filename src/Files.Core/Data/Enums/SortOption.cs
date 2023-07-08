// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	public enum SortOption : byte
	{
		/// <summary>
		/// Sort by name.
		/// </summary>
		Name,

		/// <summary>
		/// Sort by date modified.
		/// </summary>
		DateModified,

		/// <summary>
		/// Sort by date created.
		/// </summary>
		DateCreated,

		/// <summary>
		/// Sort by size.
		/// </summary>
		Size,

		/// <summary>
		/// Sort by file type.
		/// </summary>
		FileType,

		/// <summary>
		/// Sort by sync status.
		/// </summary>
		/// <remarks>
		/// Reserved for cloud drives.
		/// </remarks>
		SyncStatus,

		/// <summary>
		/// Sort by file tags.
		/// </summary>
		FileTag,

		/// <summary>
		/// Sort by original folder.
		/// </summary>
		/// <remarks>
		/// Preserved for recycle bin.
		/// </remarks>
		OriginalFolder,

		/// <summary>
		/// Sort by date deleted.
		/// </summary>
		/// <remarks>
		/// Preserved for recycle bin.
		/// </remarks>
		DateDeleted,

		/// <summary>
		/// Sort by path.
		/// </summary>
		/// <remarks>
		/// Preserved for search results.
		/// </remarks>
		Path
	}
}
