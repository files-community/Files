// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	/// <summary>
	/// Defines constants that specify option types of storage item name conflict resolve.
	/// </summary>
	public enum FileNameConflictResolveOptionType : uint
	{
		/// <summary>
		/// Generate new names.
		/// </summary>
		GenerateNewName = 0,

		/// <summary>
		/// Replace existing files.
		/// </summary>
		ReplaceExisting = 1,

		/// <summary>
		/// Skip pasting files.
		/// </summary>
		Skip = 2,

		/// <summary>
		/// No resolve option were selected.
		/// </summary>
		None = 4
	}
}
