// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.Backend.Enums
{
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
