using ColorCode;
using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class CodePreview : UserControl
	{
		private CodePreviewViewModel ViewModel { get; set; }

		public CodePreview(CodePreviewViewModel model)
		{
			ViewModel = model;
			InitializeComponent();
		}

		private void RenderDocument()
		{
			if (codeView is not null)
			{
				codeView.Blocks?.Clear();

				if (ViewModel.Item.FileExtension?.ToLowerInvariant() is ".css" or ".scss")
				{
					// Some complex CSS files can make ColorCode parsing very slow and freeze the preview pane.
					// Use our lightweight renderer for CSS/SCSS to keep preview responsive while preserving highlighting.
					CssPreviewRenderer.Render(codeView, ViewModel.TextValue ?? string.Empty, ActualTheme == ElementTheme.Dark);
					return;
				}

				if (ViewModel.ParsedCode is not { } parsedCode)
				{
					codeView.Blocks?.Add(new Paragraph { Inlines = { new Run { Text = ViewModel.TextValue ?? string.Empty } } });
					return;
				}

				var formatter = new RichTextBlockFormatter(ActualTheme, parsedCode);
				formatter.FormatRichTextBlock(ViewModel.TextValue!, ViewModel.CodeLanguage!, codeView);
			}
		}

		private void UserControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			RenderDocument();
		}

		private void UserControl_ActualThemeChanged(Microsoft.UI.Xaml.FrameworkElement sender, object args)
		{
			try
			{
				RenderDocument();
			}
			catch (Exception)
			{
			}
		}
	}
}
