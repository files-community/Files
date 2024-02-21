﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace Files.App.Dialogs
{
	/// <summary>
	/// Shows a dialog to notify user the importance of re-install temporarily.
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
