// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Contracts
{
	public interface IWidgetCardItem<T>
	{
		T Item { get; }

		BitmapImage? Thumbnail { get; }

		Task LoadCardThumbnailAsync();
	}
}
