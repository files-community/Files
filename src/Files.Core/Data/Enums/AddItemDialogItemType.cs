﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	/// <summary>
	/// Defines constants that specify item type for item creation dialog.
	/// </summary>
	public enum AddItemDialogItemType
	{
		/// <summary>
		/// Canceled an operation.
		/// </summary>
		Cancel,

		/// <summary>
		/// Item type is a folder.
		/// </summary>
		Folder,

		/// <summary>
		/// Item type is a file.
		/// </summary>
		File,

		/// <summary>
		/// Item type is a shortcut.
		/// </summary>
		Shortcut,
	}
}
