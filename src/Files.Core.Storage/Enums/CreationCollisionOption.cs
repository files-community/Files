// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Sdk.Storage.Enums
{
	public enum CreationCollisionOption : byte
	{
		GenerateUniqueName = 0,
		ReplaceExisting = 1,
		OpenIfExists = 2,
		FailIfExists = 3,
	}
}
