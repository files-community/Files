// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;

namespace Files.App.ViewModels.Previews
{
	public sealed class HtmlPreviewViewModel : BasePreviewModel
	{
		public HtmlPreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public static bool ContainsExtension(string extension)
			=> extension is ".htm" or ".html" or ".svg";

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
			=> [];
	}
}
