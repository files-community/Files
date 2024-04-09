// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;
using Windows.Storage.Streams;

namespace Files.App.ViewModels.Previews
{
	public sealed class RichTextPreviewViewModel : BasePreviewModel
	{
		public IRandomAccessStream Stream { get; set; }

		public RichTextPreviewViewModel(ListedItem item) : base(item) { }

		public static bool ContainsExtension(string extension)
			=> extension is ".rtf";

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			Stream = await Item.ItemFile.OpenReadAsync();

			return [];
		}
	}
}
