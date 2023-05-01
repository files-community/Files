// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents an ACL owner or an ACE principal type
	/// </summary>
	public enum PrincipalType
	{
		/// <summary>
		/// Unknwon principal type
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
