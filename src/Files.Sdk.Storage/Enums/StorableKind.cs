using System;

namespace Files.Sdk.Storage.Enums
{
	[Flags]
	public enum StorableKind : byte
	{
		None = 0,
		Files = 1,
		Folders = 2,
		All = Files | Folders
	}
}
