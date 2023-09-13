// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views
{
	/// <summary>
	/// Represents Splash screen page of the Files app.
	/// <br/>
	/// This page will be shown when the app launched at the first time.
	/// </summary>
	public sealed partial class SplashScreenPage : Page
	{
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
