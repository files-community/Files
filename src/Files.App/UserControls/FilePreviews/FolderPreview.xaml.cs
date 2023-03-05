using Files.App.ViewModels.UserControls.Previews;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class FolderPreview : UserControl
	{
		public FolderPreviewViewModel Model { get; set; }

		public FolderPreview(FolderPreviewViewModel model)
		{
			Model = model;
			InitializeComponent();
		}
	}
}