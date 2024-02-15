// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents group of control displays a list of quick access folders with <see cref="WidgetFolderCardItem"/>.
	/// </summary>
	public sealed partial class QuickAccessWidget : UserControl
	{
		public QuickAccessWidgetViewModel ViewModel { get; } = new();

		public QuickAccessWidget()
		{
			InitializeComponent();
		}

		public void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.ShowContextFlyout(e.OriginalSource, e);
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag.ToString() is not string path || string.IsNullOrEmpty(path))
				return;

			await ViewModel.OpenFileLocation(path);
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
			{
				string path = ((Button)sender).Tag.ToString()!;
				await NavigationHelpers.OpenPathInNewTab(path);
			}
		}
	}
}
