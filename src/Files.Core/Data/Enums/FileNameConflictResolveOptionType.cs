// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
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
