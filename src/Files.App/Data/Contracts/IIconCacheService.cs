// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Contracts
{
	internal interface IIconCacheService
	{
		Task<byte[]?> GetIconAsync(string? itemPath, string? extension, bool isFolder, uint size);

		Task<BitmapImage?> GetIconImageAsync(string? itemPath, string? extension, bool isFolder, uint size);

		void Clear();
	}
}
