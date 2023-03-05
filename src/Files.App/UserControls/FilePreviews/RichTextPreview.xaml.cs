using Files.App.ViewModels.UserControls.Previews;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class RichTextPreview : UserControl
	{
		public RichTextPreview(RichTextPreviewViewModel viewModel)
		{
			ViewModel = viewModel;
			InitializeComponent();
		}

		public RichTextPreviewViewModel ViewModel { get; set; }

		private void TextPreviewControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			TextPreviewControl.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, ViewModel.Stream);
		}
	}
}