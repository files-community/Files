// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Text.Json;

namespace Files.App.Dialogs
{
	public sealed partial class ReloadJsonParseErrorDialog : ContentDialog
	{
		public JsonException? JsonException { get; set; }

		public ReloadJsonParseErrorDialog()
		{
			InitializeComponent();
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			App.AppModel.ShouldBrokenJsonBeRefreshed = true;
		}

		private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			MainWindow.Instance.Close();
		}
	}
}
