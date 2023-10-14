// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class GeneralPage : Page
	{

		public GeneralPage()
		{
			InitializeComponent();
		}

		private void RemoveStartupPage(object sender, RoutedEventArgs e)
		{
			ViewModel.RemovePageCommand.Execute((sender as FrameworkElement).DataContext as PageOnStartupViewModel);
		}
	}
}
