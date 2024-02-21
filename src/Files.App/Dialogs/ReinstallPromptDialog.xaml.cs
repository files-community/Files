// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace Files.App.Dialogs
{
	/// <summary>
	/// Displays a dialog temporarily informing the user of the importance of reinstalling the app.
	/// </summary>
	public sealed partial class ReinstallPromptDialog : ContentDialog
	{
		public ReinstallPromptDialog()
		{
			this.InitializeComponent();
		}

		private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.ReinstallationNoticeDocsUrl));
		}
	}
}
