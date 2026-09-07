// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Defines constants that specify checkout operation type.
	/// </summary>
	public enum GitCheckoutOptions
	{
		/// <summary>
		/// Bring changes to the checking out branch.
		/// </summary>
		BringChanges,

		/// <summary>
		/// Stash changes to the checking out branch.
		/// </summary>
		StashChanges,

		/// <summary>
		/// Discard changes and check out to the branch.
		/// </summary>
		DiscardChanges,

		/// <summary>
		/// Abort merge and check out to the branch.
		/// </summary>
		AbortMerge,

		/// <summary>
		/// No operation to perform.
		/// </summary>
		None
	}
}
