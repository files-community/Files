using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Storage;

namespace Files.ViewModels.Previews
{
    public class HtmlPreviewViewModel : BasePreviewModel
    {
        public static List<string> Extensions => new List<string>() {
            ".html", ".htm",
        };

        // TODO: Move to WebView2 on WinUI 3.0 release

        public HtmlPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public string textValue;
        public string TextValue
        {
            get => textValue;
            set => SetProperty(ref textValue, value);
        }

        public async override void LoadPreviewAndDetails()
        {
            try
            {
                TextValue = await FileIO.ReadTextAsync(ItemFile);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            base.LoadSystemFileProperties();
        }
    }
}
