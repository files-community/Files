using Files.Filesystem;
using Files.ViewModels.Properties;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.ViewModels.Previews
{
    public class HtmlPreviewViewModel : BasePreviewModel
    {
        private string textValue;

        // TODO: Move to WebView2 on WinUI 3.0 release

        public HtmlPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public static List<string> Extensions => new List<string>() {
            ".html", ".htm", ".svg",
        };

        public string TextValue
        {
            get => textValue;
            set => SetProperty(ref textValue, value);
        }

        public async override Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            TextValue = await ReadFileAsText(Item.ItemFile); // await FileIO.ReadTextAsync(Item.ItemFile);
            return new List<FileProperty>();
        }
    }
}