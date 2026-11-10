// Copyright (c) Files Community
// Licensed under the MIT License.

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
			Loaded += (_, _) => ViewModel.StartListening();
			Unloaded += (_, _) => ViewModel.StopListening();
		}

		private void RemoveStartupPage(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement { DataContext: PageOnStartupViewModel page })
				ViewModel.RemovePageCommand.Execute(page);
		}
	}
}
