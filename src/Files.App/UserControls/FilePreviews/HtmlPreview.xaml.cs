using System;
using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class HtmlPreview : UserControl
	{
		public HtmlPreviewViewModel ViewModel { get; set; }

		public HtmlPreview(HtmlPreviewViewModel model)
		{
			ViewModel = model;

			InitializeComponent();
		}

		private async void WebViewControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await WebViewControl.EnsureCoreWebView2Async();
			WebViewControl.NavigateToString(ViewModel.TextValue);
		}
	}
}
