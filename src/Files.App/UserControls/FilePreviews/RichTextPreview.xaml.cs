using Files.App.ViewModels.Previews;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.IO;

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
			try
			{
				TextPreviewControl.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, ViewModel.Stream);
			}
			catch (EndOfStreamException ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}
	}
}