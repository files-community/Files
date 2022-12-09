using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class PDFPreview : UserControl
	{
		public PDFPreviewViewModel ViewModel { get; set; }

		public PDFPreview(PDFPreviewViewModel model)
		{
			ViewModel = model;

			InitializeComponent();
		}
	}
}
