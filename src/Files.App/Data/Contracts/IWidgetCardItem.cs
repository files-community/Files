// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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
