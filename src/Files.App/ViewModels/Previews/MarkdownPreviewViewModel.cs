// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;

namespace Files.App.ViewModels.Previews
{
	public class MarkdownPreviewViewModel : BasePreviewModel
	{
		private string textValue;
		public string TextValue
		{
			get => textValue;
			private set => SetProperty(ref textValue, value);
		}

		public MarkdownPreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public static bool ContainsExtension(string extension)
			=> extension is ".md" or ".markdown";

		public override async Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var text = await ReadFileAsTextAsync(Item.ItemFile);
			TextValue = text.Left(Constants.PreviewPane.TextCharacterLimit);

			return new List<FileProperty>();
		}
	}
}
