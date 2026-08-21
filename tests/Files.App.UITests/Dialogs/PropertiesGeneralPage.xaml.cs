// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UITests.Dialogs
{
	public sealed partial class PropertiesGeneralPage : Page
	{
		public PropertiesGeneralPage()
		{
			InitializeComponent();
		}

		private void ClickableCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			ClickResultText.Text = "Clicked";
		}
	}
}
