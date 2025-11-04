using Files.App.ViewModels.Previews;
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
			// Defensive check to prevent loading if stream is null or disposed
			if (ViewModel.Stream is not null)
			{
				TextPreviewControl.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, ViewModel.Stream);
			}
		}

		private void RichTextPreview_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			// Clear the document to release resources
			TextPreviewControl.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, string.Empty);

			// Call the ViewModel's unload handler to dispose the stream
			ViewModel.PreviewControlBase_Unloaded(sender, e);
		}
	}
}