// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;

namespace Files.App.UserControls.Widgets
{
	public interface IWidgetCardItem<T>
	{
		T Item { get; }

		bool HasThumbnail { get; }

		BitmapImage Thumbnail { get; }

		Task LoadCardThumbnailAsync();
	}
}