﻿namespace Files.App.Filesystem.Permissions
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
