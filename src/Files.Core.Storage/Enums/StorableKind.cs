// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.Core.Storage.Enums
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
