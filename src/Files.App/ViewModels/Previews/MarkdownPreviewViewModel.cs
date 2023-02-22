using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Files.Core.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

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

		public override async Task<List<FilePropertyViewModel>> LoadPreviewAndDetailsAsync()
		{
			var text = await ReadFileAsTextAsync(Item.ItemFile);
			TextValue = text.Left(Core.Constants.PreviewPane.TextCharacterLimit);

			return new List<FilePropertyViewModel>();
		}
	}
}
