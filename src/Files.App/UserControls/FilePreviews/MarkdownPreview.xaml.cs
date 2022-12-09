using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class MarkdownPreview : UserControl
	{
		private MarkdownPreviewViewModel ViewModel { get; set; }

		public MarkdownPreview(MarkdownPreviewViewModel model)
		{
			ViewModel = model;

			InitializeComponent();
		}
	}
}
