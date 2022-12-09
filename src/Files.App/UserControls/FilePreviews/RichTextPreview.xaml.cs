using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class RichTextPreview : UserControl
	{
		public RichTextPreviewViewModel ViewModel { get; set; }

		public RichTextPreview(RichTextPreviewViewModel viewModel)
		{
			ViewModel = viewModel;

			InitializeComponent();
		}

		private void TextPreviewControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			TextPreviewControl.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, ViewModel.Stream);
		}
	}
}
