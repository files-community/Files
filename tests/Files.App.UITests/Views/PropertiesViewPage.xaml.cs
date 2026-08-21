// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Files.App.UITests.Dialogs;

namespace Files.App.UITests.Views
{
	public sealed partial class PropertiesViewPage : Page
	{
		public PropertiesViewPage()
		{
			InitializeComponent();
		}

		private void OpenPropertiesDialogWindowButton_Click(object sender , RoutedEventArgs e)
		{
			var window = new PropertiesDialog() { Title = "Properties" };
			window.Activate();
		}
	}
}
