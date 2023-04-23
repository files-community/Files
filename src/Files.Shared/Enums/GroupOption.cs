// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Shared.Enums
{
	public enum GroupOption : byte
	{
		None,
		Name,
		DateModified,
		DateCreated,
		Size,
		FileType,
		SyncStatus, // Cloud drive
		FileTag,
		OriginalFolder, // Recycle bin
		DateDeleted, // Recycle bin
		FolderPath, // Libraries
	}
}