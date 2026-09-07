// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Represents an ACL owner or an ACE principal type
	/// </summary>
	public enum AccessControlPrincipalType
	{
		/// <summary>
		/// Unknown principal type
		/// </summary>
		Unknown,

		/// <summary>
		/// User principal type
		/// </summary>
		User,

		/// <summary>
		/// Group principal type
		/// </summary>
		Group
	};
}
