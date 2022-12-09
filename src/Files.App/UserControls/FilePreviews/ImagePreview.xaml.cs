using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class ImagePreview : UserControl
	{
		private ImagePreviewViewModel ViewModel { get; set; }

		public ImagePreview(ImagePreviewViewModel model)
		{
			ViewModel = model;

			InitializeComponent();
		}
	}
}
