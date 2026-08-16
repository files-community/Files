// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Contracts
{
	internal interface IIconCacheService
	{
		Task<byte[]?> GetIconAsync(string? itemPath, string? extension, bool isFolder);

		void Clear();
	}
}
