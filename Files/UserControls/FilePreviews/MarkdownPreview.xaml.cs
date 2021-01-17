using Files.Filesystem;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.UserControls.FilePreviews
{
    public sealed partial class MarkdownPreview : PreviewControlBase
    {
        public MarkdownPreview(ListedItem item) : base(item)
        {
            this.InitializeComponent();
        }

        public static List<string> Extensions => new List<string>() {
            ".md", ".markdown",
        };

        public override async void LoadPreviewAndDetails()
        {
            var text = await FileIO.ReadTextAsync(ItemFile);
            var displayText = text.Length < Constants.PreviewPane.TextCharacterLimit ? text : text.Remove(Constants.PreviewPane.TextCharacterLimit);
            MarkdownTextPreview.Text = displayText;
            base.LoadSystemFileProperties();
        }
    }
}
