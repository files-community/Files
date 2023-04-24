// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents inheritance flags of an ACE
	/// </summary>
	[Flags]
	public enum InheritanceFlags
	{
		/// <summary>
		/// No inheritance flags
		/// </summary>
		None = 0,

		/// <summary>
		/// Child objects that are containers, such as directories, inherit the ACE as an effective ACE.
		/// The inherited ACE is inheritable unless the NO_PROPAGATE_INHERIT_ACE bit flag is also set.
		/// </summary>
		ContainerInherit = 1,

		/// <summary>
		/// Noncontainer child objects inherit the ACE as an effective ACE.
		/// For child objects that are containers, the ACE is inherited as an inherit-only ACE
		/// unless the NO_PROPAGATE_INHERIT_ACE bit flag is also set.
		/// </summary>
		ObjectInherit = 2
	}
}
