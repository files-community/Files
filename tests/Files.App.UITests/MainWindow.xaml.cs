// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.UITests.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace Files.App.UITests
{
	public sealed partial class MainWindow : Window
	{
		private static MainWindow? _Instance;
		public static MainWindow Instance => _Instance ??= new();

		private MainWindow()
		{
			InitializeComponent();

			ExtendsContentIntoTitleBar = true;
			MainFrame.Navigate(typeof(MainPage));

			// Set the toggle state for theme change button
			if (Content is FrameworkElement element)
			{
				if (element.ActualTheme is ElementTheme.Light)
				{
					AppThemeChangeToggleButton.IsChecked = true;
					AppThemeGlyph.Glyph = "\uE706"; // Sun
				}
				else
				{
					AppThemeChangeToggleButton.IsChecked = false;
					AppThemeGlyph.Glyph = "\uE708"; // Moon
				}
			}
		}

		private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
				return;

			MainFrame.Navigate(
				tag switch
				{
					nameof(ThemedIconPage) => typeof(ThemedIconPage),
					nameof(ToolbarPage) => typeof(ToolbarPage),
					nameof(StorageControlsPage) => typeof(StorageControlsPage),
					nameof(SidebarViewPage) => typeof(SidebarViewPage),
					nameof(OmnibarPage) => typeof(OmnibarPage),
					nameof(BreadcrumbBarPage) => typeof(BreadcrumbBarPage),
					_ => throw new InvalidOperationException("There's no applicable page associated with the given key."),
				});

			MainNavigationView.Header = item.Content.ToString();
		}

		private void AppThemeChangeToggleButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not ToggleButton toggleButton ||
				Content is not FrameworkElement element)
				return;

			if (toggleButton.IsChecked is true)
			{
				element.RequestedTheme = ElementTheme.Light;
				AppThemeGlyph.Glyph = "\uE706"; // Sun
			}
			else
			{
				element.RequestedTheme = ElementTheme.Dark;
				AppThemeGlyph.Glyph = "\uE708"; // Moon
			}
		}
	}
}
