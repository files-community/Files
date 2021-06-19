using Files.ViewModels.Previews;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class PDFPreview : UserControl
    {
        public PDFPreview(PDFPreviewViewModel model)
        {
            ViewModel = model;
            InitializeComponent();
        }

        public PDFPreviewViewModel ViewModel { get; set; }
    }
}