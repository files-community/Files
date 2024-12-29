// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Contracts
{
	public interface IWidgetCardItem<T>
	{
		T Item { get; }

		BitmapImage Thumbnail { get; }

		Task LoadCardThumbnailAsync();
	}
}
