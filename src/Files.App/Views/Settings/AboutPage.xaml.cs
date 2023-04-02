using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;

namespace Files.App.Views.Settings
{
	public sealed partial class AboutPage : Page
	{
		public AboutPage()
		{
			InitializeComponent();
		}

		private void ThirdPartyLicenses_Click(object sender, bool e)
		{
			if (e && ViewModel.ThirdPartyNotices is null)
				ViewModel.LoadThirdPartyNotices();
		}

		private async void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri? link))
				await Launcher.LaunchUriAsync(link);
		}
	}
}
