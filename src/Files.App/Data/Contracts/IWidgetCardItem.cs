// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents widget card item.
	/// </summary>
	public interface IWidgetCardItem
	{
		string? Path { get; }

		BitmapImage? Thumbnail { get; }

		Task LoadCardThumbnailAsync();
	}
}
