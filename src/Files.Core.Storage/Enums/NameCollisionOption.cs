// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Sdk.Storage.Enums
{
	public enum NameCollisionOption : byte
	{
		GenerateUniqueName = 0,
		ReplaceExisting = 1,
		FailIfExists = 2
	}
}
