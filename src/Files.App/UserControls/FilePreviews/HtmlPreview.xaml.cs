using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;
using System.IO;

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
			WebViewControl.CoreWebView2.SetVirtualHostNameToFolderMapping(
				"preview.files",
				Path.GetDirectoryName(ViewModel.Item.ItemPath),
				Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.DenyCors);
			WebViewControl.Source = new Uri("http://preview.files/" + ViewModel.Item.Name);
		}
	}
}