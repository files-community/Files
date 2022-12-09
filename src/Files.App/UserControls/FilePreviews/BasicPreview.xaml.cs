using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class BasicPreview : UserControl
	{
		public BasePreviewModel Model { get; set; }

		public BasicPreview(BasePreviewModel model)
		{
			Model = model;
			this.InitializeComponent();
		}
	}
}
