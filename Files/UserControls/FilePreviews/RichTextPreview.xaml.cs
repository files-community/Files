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
    public sealed partial class RichTextPreview : UserControl
    {
        public static List<string> Extensions => new List<string>() {
            ".rtf", ".doc"
        };

        public RichTextPreview(ListedItem item)
        {
            this.InitializeComponent();
            SetFile(item);
        }

        public async void SetFile(ListedItem item)
        {
            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
            var stream = await file.OpenReadAsync();
            TextPreviewControl.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, stream);
        }
    }
}
