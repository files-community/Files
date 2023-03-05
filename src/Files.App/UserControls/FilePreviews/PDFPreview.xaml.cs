using Files.App.ViewModels.UserControls.Previews;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.FilePreviews
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