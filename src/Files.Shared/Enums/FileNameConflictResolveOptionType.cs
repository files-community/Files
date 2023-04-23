// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Shared.Enums
{
	public enum FileNameConflictResolveOptionType : uint
	{
		GenerateNewName = 0,
		ReplaceExisting = 1,
		Skip = 2,
		None = 4
	}
}