// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	public enum GroupOption : byte
	{
		/// <summary>
		/// No grouping.
		/// </summary>
		None,

		/// <summary>
		/// Group by name
		/// </summary>
		Name,

		/// <summary>
		/// Group by date modified.
		/// </summary>
		DateModified,

		/// <summary>
		/// Group by date created.
		/// </summary>
		DateCreated,

		/// <summary>
		/// Group by size.
		/// </summary>
		Size,

		/// <summary>
		/// Group by file type.
		/// </summary>
		FileType,

		/// <summary>
		/// Group by sync status.
		/// </summary>
		/// <remarks>
		/// Preserved for cloud drives.
		/// </remarks>
		SyncStatus,

		/// <summary>
		/// Group by file tags.
		/// </summary>
		FileTag,

		/// <summary>
		/// Group by original folder.
		/// </summary>
		/// <remarks>
		/// Preserved for recycle bin.
		/// </remarks>
		OriginalFolder,

		/// <summary>
		/// Group by date deleted.
		/// </summary>
		/// <remarks>
		/// Preserved for recycle bin.
		/// </remarks>
		DateDeleted,

		/// <summary>
		/// Group by folder path.
		/// </summary>
		/// <remarks>
		/// Preserved for libraries.
		/// </remarks>
		FolderPath,
	}
}
