using Files.ViewModels.Previews;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class RichTextPreview : UserControl
    {
        public RichTextPreview(RichTextPreviewViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        public RichTextPreviewViewModel ViewModel { get; set; }

        private void TextPreviewControl_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            TextPreviewControl.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, ViewModel.Stream);
        }
    }
}