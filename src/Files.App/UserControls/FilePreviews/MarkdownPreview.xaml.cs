// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Controls;
using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using WinRT;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class MarkdownPreview : UserControl
	{
		private MarkdownPreviewViewModel ViewModel { get; set; }

		[DynamicWindowsRuntimeCast(typeof(Brush))]
		public MarkdownPreview(MarkdownPreviewViewModel model)
		{
			ViewModel = model;
			InitializeComponent();

			// Workaround for https://github.com/CommunityToolkit/Labs-Windows/issues/611
			PreviewMarkdownTextBlock.Config = MarkdownConfig.Default;
			var headingForeground = (Brush)Application.Current.Resources["TextControlForeground"];
			var themes = PreviewMarkdownTextBlock.Config.Themes;
			themes.H1Foreground = headingForeground;
			themes.H2Foreground = headingForeground;
			themes.H3Foreground = headingForeground;
			themes.H4Foreground = headingForeground;
			themes.H5Foreground = headingForeground;
			themes.H6Foreground = headingForeground;
		}

		private async void PreviewMarkdownTextBlock_OnLinkClicked(object sender, LinkClickedEventArgs e)
		{
			await Launcher.LaunchUriAsync(e.Uri);
		}
	}
}
