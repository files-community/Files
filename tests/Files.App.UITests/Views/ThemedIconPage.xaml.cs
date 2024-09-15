// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UITests.Views
{
	public sealed partial class ThemedIconPage : Page
	{
		public ThemedIconPage()
		{
			InitializeComponent();
		}

		void ButtonTestEnabledStates_Click(object sender, RoutedEventArgs e)
		{
			if (AppBarButtonDisable.IsEnabled)
				AppBarButtonDisable.IsEnabled = false;
			else if (!AppBarButtonDisable.IsEnabled)
				AppBarButtonDisable.IsEnabled = true;

			if (AppBarButtonDisable2.IsEnabled)
				AppBarButtonDisable2.IsEnabled = false;
			else if (!AppBarButtonDisable2.IsEnabled)
				AppBarButtonDisable2.IsEnabled = true;
		}
	}
}
