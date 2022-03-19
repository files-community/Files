﻿using Files.ViewModels.Previews;
using Windows.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.UserControls.FilePreviews
{
    public sealed partial class TextPreview : UserControl
    {
        public TextPreview(TextPreviewViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        public TextPreviewViewModel ViewModel { get; set; }
    }
}