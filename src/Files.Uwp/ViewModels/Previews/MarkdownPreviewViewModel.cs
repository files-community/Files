using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Uwp.ViewModels.Previews
{
    public class MarkdownPreviewViewModel : BasePreviewModel
    {
        private string textValue;
        public string TextValue
        {
            get => textValue;
            private set => SetProperty(ref textValue, value);
        }

        public MarkdownPreviewViewModel(ListedItem item) : base(item) {}

        public static bool ContainsExtensions(string extension) => extension is ".md" or ".markdown";

        public override async Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var text = await ReadFileAsText(Item.ItemFile);
            TextValue = text.Length < Constants.PreviewPane.TextCharacterLimit
                ? text
                : text.Remove(Constants.PreviewPane.TextCharacterLimit);
            return new List<FileProperty>();
        }
    }
}