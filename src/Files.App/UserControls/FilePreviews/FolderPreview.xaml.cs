using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class FolderPreview : UserControl
	{
		public FolderPreviewViewModel Model { get; set; }

		public FolderPreview(FolderPreviewViewModel model)
		{
			Model = model;

			this.InitializeComponent();
		}
	}
}
