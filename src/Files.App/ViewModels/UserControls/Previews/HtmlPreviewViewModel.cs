// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;

namespace Files.App.ViewModels.Previews
{
	public sealed partial class HtmlPreviewViewModel : BasePreviewModel
	{
		public HtmlPreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
			=> [];
	}
}
