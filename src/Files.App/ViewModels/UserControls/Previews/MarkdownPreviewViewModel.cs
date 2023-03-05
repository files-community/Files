using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Files.Shared.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.App.ViewModels.UserControls.Previews
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
