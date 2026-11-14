using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class RichTextPreview : UserControl
	{
		private int loadVersion;

		public RichTextPreview(RichTextPreviewViewModel viewModel)
		{
			ViewModel = viewModel;
			InitializeComponent();
		}

		public RichTextPreviewViewModel ViewModel { get; set; }

		private async void TextPreviewControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var version = ++loadVersion;
			LoadingIndicator.IsActive = true;
			PreviewError.Visibility = Visibility.Collapsed;
			TextPreviewControl.Visibility = Visibility.Visible;
			bool documentLoaded = false;

			var loaded = await SafetyExtensions.IgnoreExceptions(() => DispatcherQueue.EnqueueDeferredAsync(() =>
			{
				if (version != loadVersion)
					return Task.CompletedTask;

				TextPreviewControl.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, ViewModel.Stream);
				documentLoaded = true;
				return Task.CompletedTask;
			}), App.Logger);

			if (version != loadVersion)
				return;

			LoadingIndicator.IsActive = false;
			if (!loaded || !documentLoaded)
			{
				TextPreviewControl.Visibility = Visibility.Collapsed;
				PreviewError.Visibility = Visibility.Visible;
			}
		}

		private void RichTextPreview_Unloaded(object sender, RoutedEventArgs e)
		{
			loadVersion++;
			LoadingIndicator.IsActive = false;
		}
	}
}