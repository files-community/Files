using Files.Filesystem;
using Files.ViewModels.Previews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class HtmlPreview : UserControl
    {
        public HtmlPreviewViewModel ViewModel { get; set; }

        // TODO: Move to WebView2 on WinUI 3.0 release

        public HtmlPreview(HtmlPreviewViewModel model)
        {
            ViewModel = model;
            ViewModel.LoadedEvent += ViewModel_LoadedEvent;
            this.InitializeComponent();
        }

        private void ViewModel_LoadedEvent(object sender, EventArgs e)
        {
            WebViewControl.NavigateToString(ViewModel.TextValue);
        }
    }
}
