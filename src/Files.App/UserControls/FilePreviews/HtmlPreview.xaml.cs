using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class HtmlPreview : UserControl
	{
		public HtmlPreview(HtmlPreviewViewModel model)
		{
			ViewModel = model;
			InitializeComponent();
		}

		public HtmlPreviewViewModel ViewModel { get; set; }

		private async void WebViewControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await WebViewControl.EnsureCoreWebView2Async();
			WebViewControl.NavigateToString(ViewModel.TextValue);
		}
	}
}