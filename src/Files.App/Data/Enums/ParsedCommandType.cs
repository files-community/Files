// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Defines constants that specify parsed command line item type on Windows.
	/// </summary>
	public enum ParsedCommandType
	{
		/// <summary>
		/// Unknown command type.
		/// </summary>
		Unknown,

		/// <summary>
		/// Open directory command type
		/// </summary>
		OpenDirectory,

		/// <summary>
		/// Open path command type
		/// </summary>
		OpenPath,

		/// <summary>
		/// Explorer shell command type
		/// </summary>
		ExplorerShellCommand,

		/// <summary>
		/// Output path command type
		/// </summary>
		OutputPath,

		/// <summary>
		/// Select path command type
		/// </summary>
		SelectItem,

		/// <summary>
		/// Tag files command type
		/// </summary>
		TagFiles
	}
}
