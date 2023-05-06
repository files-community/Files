// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents propagation flags of an ACE
	/// </summary>
	[Flags]
	public enum PropagationFlags
	{
		/// <summary>
		/// No propagation flags
		/// </summary>
		None = 0,

		/// <summary>
		/// If the ACE is inherited by a child object, the system clears the OBJECT_INHERIT_ACE and CONTAINER_INHERIT_ACE flags in the inherited ACE.
		/// This prevents the ACE from being inherited by subsequent generations of objects.
		/// </summary>
		NoPropagateInherit = 1,

		/// <summary>
		/// Indicates an inherit-only ACE, which does not control access to the object to which it is attached.
		/// If this flag is not set, the ACE is an effective ACE which controls access to the object to which it is attached.
		/// Both effective and inherit-only ACEs can be inherited depending on the state of the other inheritance flags.
		/// </summary>
		InheritOnly = 2
	}
}
