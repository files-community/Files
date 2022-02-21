using ColorCode;
using Files.ViewModels.Previews;
using System;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class CodePreview : UserControl
    {
        private RichTextBlockFormatter formatter;
        private bool rendered;

        private CodePreviewViewModel ViewModel { get; set; }

        public CodePreview(CodePreviewViewModel model)
        {
            ViewModel = model;
            this.InitializeComponent();
        }

        private void RenderDocument()
        {
            if (codeView != null)
            {
                codeView.Blocks?.Clear();
                formatter = new RichTextBlockFormatter(ActualTheme);

                formatter.FormatRichTextBlock(ViewModel.TextValue, ViewModel.CodeLanguage, codeView);
                rendered = true;
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
                rendered = false;
                RenderDocument();
            }
            catch (Exception)
            {
            }
        }
    }
}