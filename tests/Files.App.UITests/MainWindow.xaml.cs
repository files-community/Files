// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UITests.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UITests
{
	public sealed partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			MainFrame.Navigate(typeof(MainPage));
		}

		private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
				return;

			switch (tag)
			{
				case "ThemedIconPage":
					MainFrame.Navigate(typeof(ThemedIconPage));
					break;
			}
		}
	}
}
