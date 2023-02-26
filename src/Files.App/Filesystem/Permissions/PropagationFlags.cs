using System;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents propagation flags of an ACE
	/// </summary>
	[Flags]
	public enum PropagationFlags
	{
		None = 0,

		NoPropagateInherit = 1,

		InheritOnly = 2
	}
}
