using Files.Filesystem;
using Files.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public sealed partial class MarkdownPreview : UserControl
    {
        MarkdownPreviewViewModel ViewModel { get; set; }

        public MarkdownPreview(MarkdownPreviewViewModel model)
        {
            ViewModel = model;
            this.InitializeComponent();
        }
    }
}
