using System;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents inheritance flags of an ACE
	/// </summary>
	[Flags]
	public enum InheritanceFlags
	{
		None = 0,

		ContainerInherit = 1,

		ObjectInherit = 2
	}
}
