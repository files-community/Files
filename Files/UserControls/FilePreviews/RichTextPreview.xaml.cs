using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public sealed partial class RichTextPreview : PreviewControlBase
    {
        public static List<string> Extensions => new List<string>() {
            ".rtf", ".doc"
        };

        public RichTextPreview(ListedItem item) : base(item)
        {
            this.InitializeComponent();
        }

        public async override void LoadPreviewAndDetails()
        {
            try
            {
                var stream = await ItemFile.OpenReadAsync();
                TextPreviewControl.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, stream);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            LoadSystemFileProperties();
        }
    }
}
