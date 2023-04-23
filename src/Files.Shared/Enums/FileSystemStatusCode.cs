// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.Shared.Enums
{
	[Flags]
	public enum FileSystemStatusCode
	{
		Success = 0,
		Generic = 1,
		Unauthorized = 2,
		NotFound = 4,
		InUse = 8,
		NameTooLong = 16,
		AlreadyExists = 32,
		NotAFolder = 64,
		NotAFile = 128,
		ReadOnly = 256,
		PropertyLoss = 512,
		InProgress = 1024
	}
}