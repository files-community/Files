// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views
{
	/// <summary>
	/// Display the app splash screen.
	/// </summary>
	public sealed partial class SplashScreenPage : Page
	{
		private string BranchLabel =>
			ApplicationService.AppEnvironment switch
			{
				AppEnvironment.Dev => "Dev",
				AppEnvironment.Preview => "Preview",
				_ => string.Empty,
			};

		public SplashScreenPage()
		{
			InitializeComponent();
		}

		private void Image_ImageOpened(object sender, RoutedEventArgs e)
		{
			App.SplashScreenLoadingTCS?.TrySetResult();
		}

		private void Image_ImageFailed(object sender, RoutedEventArgs e)
		{
			App.SplashScreenLoadingTCS?.TrySetResult();
		}
	}
}
