using System;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents access right flags of an ACE
	/// </summary>
	[Flags]
	public enum AccessMask
	{
		NULL = 0,

		ReadData = 1,

		ListDirectory = 1,

		WriteData = 2,

		CreateFiles = 2,

		AppendData = 4,

		CreateDirectories = 4,

		ReadExtendedAttributes = 8,

		WriteExtendedAttributes = 16,

		ExecuteFile = 32,

		Traverse = 32,

		DeleteSubdirectoriesAndFiles = 64,

		ReadAttributes = 128,

		WriteAttributes = 256,

		Write = 278,

		Delete = 65536,

		ReadPermissions = 131072,

		Read = 131209,

		ReadAndExecute = 131241,

		Modify = 197055,

		ChangePermissions = 262144,

		TakeOwnership = 524288,

		Synchronize = 1048576,

		FullControl = 2032127
	}
}
