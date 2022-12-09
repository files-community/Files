using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class TextPreview : UserControl
	{
		public TextPreviewViewModel ViewModel { get; set; }

		public TextPreview(TextPreviewViewModel viewModel)
		{
			ViewModel = viewModel;
			InitializeComponent();
		}
	}
}
