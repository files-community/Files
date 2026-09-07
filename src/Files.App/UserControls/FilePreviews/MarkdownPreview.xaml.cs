// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Labs.WinUI.MarkdownTextBlock;
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
			PreviewMarkdownTextBlock.Config.Themes.HeadingForeground = (Brush)Application.Current.Resources["TextControlForeground"];
		}

		private async void PreviewMarkdownTextBlock_OnLinkClicked(object sender, CommunityToolkit.Labs.WinUI.MarkdownTextBlock.LinkClickedEventArgs e)
		{
			await Launcher.LaunchUriAsync(e.Uri);
		}
	}
}
