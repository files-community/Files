using Files.Filesystem;
using Files.ViewModels.Previews;
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
    public sealed partial class RichTextPreview : UserControl
    {
        public RichTextPreviewViewModel ViewModel { get; set; }
        public RichTextPreview(RichTextPreviewViewModel viewModel)
        {
            viewModel.LoadedEvent += ViewModel_LoadedEvent;
            ViewModel = viewModel;
            this.InitializeComponent();
        }

        private void ViewModel_LoadedEvent(object sender, EventArgs e)
        {
            TextPreviewControl.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, ViewModel.Stream);
        }
    }
}
