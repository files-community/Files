﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	/// <summary>
	/// Defines constants that specify item type of the file system on Windows.
	/// </summary>
	public enum FilesystemItemType : byte
	{
		/// <summary>
		/// The item is a directory.
		/// </summary>
		Directory = 0,

		/// <summary>
		/// The item is a file.
		/// </summary>
		File = 1,

		/// <summary>
		/// The item is a symlink.
		/// </summary>
		[Obsolete("The symlink has no use for now here.")]
		Symlink = 2,

		/// <summary>
		/// The item is a library.
		/// </summary>
		Library = 3,
	}
}
