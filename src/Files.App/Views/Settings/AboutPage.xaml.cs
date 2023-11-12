// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace Files.App.Views.Settings
{
	public sealed partial class AboutPage : Page
	{
		public AboutPage()
		{
			InitializeComponent();
		}

		private async void ThirdPartyLicenses_Click(object sender, bool e)
		{
			if (e && ViewModel.ThirdPartyNotices is null)
				await ViewModel.LoadThirdPartyNoticesAsync();
		}

		private async void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri? link))
				await Launcher.LaunchUriAsync(link);
		}
	}
}
