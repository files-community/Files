// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.Core.Storage.Enums
{
	/// <summary>
	/// Defines constants that specify storage item kind.
	/// </summary>
	[Flags]
	public enum StorableKind : byte
	{
		/// <summary>
		/// Unknown storable item type
		/// </summary>
		None = 0,

		/// <summary>
		/// File storable item type
		/// </summary>
		Files = 1,

		/// <summary>
		/// Folder storable item type
		/// </summary>
		Folders = 2,

		/// <summary>
		/// All storable item type
		/// </summary>
		All = Files | Folders
	}
}
