using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class HtmlPreview : PreviewControlBase
    {
        public static List<string> Extensions => new List<string>() {
            ".html", ".htm",
        };

        // TODO: Move to WebView2 on WinUI 3.0 release

        public HtmlPreview(ListedItem item) : base(item)
        {
            this.InitializeComponent();
        }


        public async override void LoadPreviewAndDetails()
        {
            var text = await FileIO.ReadTextAsync(ItemFile);
            WebViewControl.NavigateToString(text);
            base.LoadSystemFileProperties();
        }
    }
}
