﻿using ColorCode;
using Files.Uwp.ViewModels.Previews;
using System;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.Uwp.UserControls.FilePreviews
{
    public sealed partial class CodePreview : UserControl
    {
        private RichTextBlockFormatter formatter;

        private CodePreviewViewModel ViewModel { get; set; }

        public CodePreview(CodePreviewViewModel model)
        {
            ViewModel = model;
            InitializeComponent();
        }

        private void RenderDocument()
        {
            if (codeView != null)
            {
                codeView.Blocks?.Clear();
                formatter = new RichTextBlockFormatter(ActualTheme);

                formatter.FormatRichTextBlock(ViewModel.TextValue, ViewModel.CodeLanguage, codeView);
            }
        }

        private void UserControl_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            RenderDocument();
        }

        private void UserControl_ActualThemeChanged(Windows.UI.Xaml.FrameworkElement sender, object args)
        {
            try
            {
                RenderDocument();
            }
            catch (Exception)
            {
            }
        }
    }
}