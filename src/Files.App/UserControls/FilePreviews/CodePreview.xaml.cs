using ColorCode;
using Files.App.ViewModels.UserControls.Previews;
using Microsoft.UI.Xaml.Controls;
using System;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class CodePreview : UserControl
	{
		private RichTextBlockFormatter formatter;

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
				formatter = new RichTextBlockFormatter(ActualTheme);

				formatter.FormatRichTextBlock(ViewModel.TextValue, ViewModel.CodeLanguage, codeView);
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